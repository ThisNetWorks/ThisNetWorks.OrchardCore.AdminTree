using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrchardCore;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Navigation;
using OrchardCore.Taxonomies.Models;
using ThisNetWorks.OrchardCore.AdminTree.ViewModels;

namespace ThisNetWorks.OrchardCore.AdminTree.Drivers
{
    public class TaxonomyPartDisplayDriver : ContentPartDisplayDriver<TaxonomyPart>
    {
        private readonly IOrchardHelper _orchardHelper;

        public TaxonomyPartDisplayDriver(
            IOrchardHelper orchardHelper
            )
        {
            _orchardHelper = orchardHelper;
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
                    model.TermPart = termContentItem.As<TermPart>();
                    model.ContentItems = (await _orchardHelper
                        .QueryCategorizedContentItemsAsync(q =>
                            q.Where(x => x.TaxonomyContentItemId == taxonomyPart.ContentItem.ContentItemId &&
                                x.TermContentItemId == termContentItem.ContentItemId))).ToList();
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