using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Models;
using OrchardCore.Taxonomies.Settings;
using ThisNetworks.OrchardCore.AdminTree.Models;
using ThisNetworks.OrchardCore.AdminTree.ViewModels;

namespace ThisNetWorks.OrchardCore.AdminTree.Drivers
{
    public class TermContainerPartDisplayDriver : ContentPartDisplayDriver<TermContainerPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IStringLocalizer S;
        public TermContainerPartDisplayDriver(
            IContentDefinitionManager contentDefinitionManager,
            IStringLocalizer<TermContainerPartDisplayDriver> stringLocalizer
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            S = stringLocalizer;
        }

        public override async Task<IDisplayResult> EditAsync(TermContainerPart part, BuildPartEditorContext context)
        {
            var taxonomyIdViewModel = new TaxonomyPartIdViewModel();

            if (await context.Updater.TryUpdateModelAsync(taxonomyIdViewModel)
                && !String.IsNullOrEmpty(taxonomyIdViewModel.TaxonomyContentItemId))
            {

                // By design only supports the first field using the contained editor.
                var selectables = _contentDefinitionManager.ListTypeDefinitions()
                    .SelectMany(ctd => ctd.Parts.Where(p => p.PartDefinition.Fields.Any(f => f.FieldDefinition.Name == nameof(TaxonomyField) &&
                        f.GetSettings<TaxonomyFieldSettings>().TaxonomyContentItemId == taxonomyIdViewModel.TaxonomyContentItemId &&
                        f.Editor() == "Contained")));

                var entries = selectables.Select(ctd => new ContentTypeEntryViewModel
                {
                    ContentTypeName = ctd.ContentTypeDefinition.Name,
                    IsSelected = part.ContainedContentTypes.Any(selected => String.Equals(selected, ctd.ContentTypeDefinition.Name, StringComparison.OrdinalIgnoreCase)),
                    ContentTypeDisplayName = ctd.ContentTypeDefinition.DisplayName
                }).ToArray();

                return Initialize<TermContainerPartViewModel>("TermContainerPart", model =>
                {
                    model.TermContainerPart = part;
                    model.Multiple = part.Multiple;
                    model.ContainedContentTypes = entries;
                });
            } else
            {
                return null;
            }
        }

        public override async Task<IDisplayResult> UpdateAsync(TermContainerPart part, IUpdateModel updater, UpdatePartEditorContext context)
        {
            var viewModel = new TermContainerPartViewModel();

            if (await context.Updater.TryUpdateModelAsync(viewModel, Prefix,
                m => m.Multiple,
                m => m.ContainedContentTypes
                ))
            {
                if (viewModel.ContainedContentTypes == null || viewModel.ContainedContentTypes.Length == 0)
                {
                    context.Updater.ModelState.AddModelError(nameof(viewModel.ContainedContentTypes), S["At least one content type must be selected."]);
                }
                else
                {
                    part.ContainedContentTypes = viewModel.ContainedContentTypes
                        .Where(x => x.IsSelected == true)
                        .Select(x => x.ContentTypeName)
                        .ToArray();
                    part.Multiple = viewModel.Multiple;
                }
            }

            return await EditAsync(part, context);
        }
    }
}
