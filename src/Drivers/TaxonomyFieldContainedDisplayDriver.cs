using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Taxonomies.Drivers;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Models;
using OrchardCore.Taxonomies.Settings;
using OrchardCore.Taxonomies.ViewModels;
using ThisNetWorks.OrchardCore.AdminTree.ViewModels;

namespace ThisNetWorks.OrchardCore.AdminTree.Drivers
{
    public class TaxonomyFieldContainedDisplayDriver : ContentFieldDisplayDriver<TaxonomyField>
    {
        private readonly IContentManager _contentManager;
        private readonly IStringLocalizer S;

        public TaxonomyFieldContainedDisplayDriver(
            IContentManager contentManager,
            IStringLocalizer<TaxonomyFieldContainedDisplayDriver> s)
        {
            _contentManager = contentManager;
            S = s;
        }

        public override IDisplayResult Edit(TaxonomyField field, BuildFieldEditorContext context)
        {
            if (String.Equals(context.PartFieldDefinition.Editor(), "Contained", StringComparison.OrdinalIgnoreCase))
            {
                // if term entries. selected = 0 see if it's in the query string?
                return Initialize<EditTaxonomyFieldViewModel>(GetEditorShapeType(context), async model =>
                {
                    var settings = context.PartFieldDefinition.GetSettings<TaxonomyFieldSettings>();
                    model.Taxonomy = await _contentManager.GetAsync(settings.TaxonomyContentItemId, VersionOptions.Latest);

                    if (model.Taxonomy != null)
                    {
                        var termEntries = new List<TermEntry>();
                        TaxonomyFieldDriverHelper.PopulateTermEntries(termEntries, field, model.Taxonomy.As<TaxonomyPart>().Terms, 0);

                        model.TermEntries = termEntries;
                        var containedViewModel = new TaxonomyFieldContainedViewModel();

                        if (await context.Updater.TryUpdateModelAsync(containedViewModel, context.PartFieldDefinition.Name)
                            && !String.IsNullOrEmpty(containedViewModel.TermContentItemId))
                        {
                            var creatingTermEntry = model.TermEntries.FirstOrDefault(te => String.Equals(te.ContentItemId, containedViewModel.TermContentItemId));
                            if (creatingTermEntry != null)
                            {
                                creatingTermEntry.Selected = true;
                            }
                        }

                        model.UniqueValue = termEntries.FirstOrDefault(x => x.Selected)?.ContentItemId;
                    }

                    model.Field = field;
                    model.Part = context.ContentPart;
                    model.PartFieldDefinition = context.PartFieldDefinition;
                });
            }

            return null;
        }

        public override async Task<IDisplayResult> UpdateAsync(TaxonomyField field, IUpdateModel updater, UpdateFieldEditorContext context)
        {
            if (String.Equals(context.PartFieldDefinition.Editor(), "Contained", StringComparison.OrdinalIgnoreCase))
            {
                var model = new EditTaxonomyFieldViewModel();

                if (await updater.TryUpdateModelAsync(model, Prefix))
                {
                    var settings = context.PartFieldDefinition.GetSettings<TaxonomyFieldSettings>();

                    field.TaxonomyContentItemId = settings.TaxonomyContentItemId;
                    field.TermContentItemIds = model.TermEntries.Where(x => x.Selected).Select(x => x.ContentItemId).ToArray();

                    if (settings.Unique && !String.IsNullOrEmpty(model.UniqueValue))
                    {
                        field.TermContentItemIds = new[] { model.UniqueValue };
                    }

                    if (settings.Required && field.TermContentItemIds.Length == 0)
                    {
                        updater.ModelState.AddModelError(
                            nameof(EditTaxonomyFieldViewModel.TermEntries),
                            S["A value is required for '{0}'", context.PartFieldDefinition.Name]);
                    }
                }
            }

            return Edit(field, context);
        }
    }
}
