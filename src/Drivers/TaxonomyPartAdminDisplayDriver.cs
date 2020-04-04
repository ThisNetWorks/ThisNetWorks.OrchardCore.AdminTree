using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Navigation;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Models;
using OrchardCore.Taxonomies.Settings;
using ThisNetworks.OrchardCore.AdminTree.Models;
using ThisNetWorks.OrchardCore.AdminTree.ViewModels;

namespace ThisNetWorks.OrchardCore.AdminTree.Drivers
{
    public class TaxonomyPartAdminDisplayDriver : ContentPartDisplayDriver<TaxonomyPart>
    {
        private readonly IOrchardHelper _orchardHelper;
        private readonly IContentDefinitionManager _contentDefinitionManager;

        public TaxonomyPartAdminDisplayDriver(
            IOrchardHelper orchardHelper,
            IContentDefinitionManager contentDefinitionManager
            )
        {
            _orchardHelper = orchardHelper;
            _contentDefinitionManager = contentDefinitionManager;
        }

        public override IDisplayResult Display(TaxonomyPart taxonomyPart, BuildPartDisplayContext context)
        {
            return Initialize<DisplayTaxonomyPartViewModel>("TaxonomyPart", async model =>
            {
                await context.Updater.TryUpdateModelAsync(model, nameof(TaxonomyPart));

                var pager = await GetPagerAsync(context.Updater, taxonomyPart);

                var termContentItem = await _orchardHelper.GetTaxonomyTermAsync(taxonomyPart.ContentItem.ContentItemId, model.TermContentItemId);
                if (termContentItem != null)
                {
                    model.TermContentItem = termContentItem;
                    model.ContentItems = (await _orchardHelper
                        .QueryCategorizedContentItemsAsync(q =>
                            q.Where(x => x.TaxonomyContentItemId == taxonomyPart.ContentItem.ContentItemId &&
                                x.TermContentItemId == termContentItem.ContentItemId))).ToList();
                }

                var termContainer = termContentItem.As<TermContainerPart>();
                if (termContainer != null)
                {
                    var ctpds = termContainer.ContainedContentTypes.Select(contentType => _contentDefinitionManager.GetTypeDefinition(contentType));

                    // By design only supports the first field using the contained editor.
                    var containables = ctpds
                        .SelectMany(ctd => ctd.Parts.Where(p => p.PartDefinition.Fields.Any(f => f.FieldDefinition.Name == nameof(TaxonomyField) &&
                            f.GetSettings<TaxonomyFieldSettings>().TaxonomyContentItemId == taxonomyPart.ContentItem.ContentItemId &&
                            f.Editor() == "Contained")));

                    var entries = new List<ContentTypeEntry>();

                    foreach (var ctd in containables)
                    {
                        if (!termContainer.ContainedContentTypes.Any(ct => ct == ctd.ContentTypeDefinition.Name))
                        {
                            continue;
                        }

                        var entry = new ContentTypeEntry
                        {
                            ContentTypeDefinition = ctd.ContentTypeDefinition
                        };

                        // Find first field
                        foreach (var part in ctd.ContentTypeDefinition.Parts)
                        {
                            var field = part.PartDefinition.Fields.FirstOrDefault(f => f.FieldDefinition.Name == nameof(TaxonomyField) &&
                                f.GetSettings<TaxonomyFieldSettings>().TaxonomyContentItemId == taxonomyPart.ContentItem.ContentItemId &&
                                f.Editor() == "Contained");
                            if (field != null)
                            {
                                entry.TaxonomyFieldName = field.Name;
                                break;
                            }
                        }
                        if (!String.IsNullOrEmpty(entry.TaxonomyFieldName))
                        {
                            entries.Add(entry);
                        }
                    }
                    model.ContainedContentTypeDefinitions = entries;
                }
                model.TaxonomyPart = taxonomyPart;
                model.Context = context;
                model.Pager = await context.New.PagerSlim(pager);
            })
            .Location("DetailAdmin", "Content:10");
        }

        private async Task<PagerSlim> GetPagerAsync(IUpdateModel updater, TaxonomyPart part)
        {
            //var settings = GetSettings(part);
            PagerSlimParameters pagerParameters = new PagerSlimParameters();
            await updater.TryUpdateModelAsync(pagerParameters);

            PagerSlim pager = new PagerSlim(pagerParameters, 10);

            return pager;
        }
    }
}
