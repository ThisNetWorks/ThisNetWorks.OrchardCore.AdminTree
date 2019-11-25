using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Navigation;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class TaxonomyContentsAdminNodeDriver : DisplayDriver<MenuItem, TaxonomyContentsAdminNode>
    {
        public override IDisplayResult Display(TaxonomyContentsAdminNode treeNode)
        {
            return Combine(
                View("TaxonomyContentsAdminNode_Fields_TreeSummary", treeNode).Location("TreeSummary", "Content"),
                View("TaxonomyContentsAdminNode_Fields_TreeThumbnail", treeNode).Location("TreeThumbnail", "Content")
            );
        }

        public override IDisplayResult Edit(TaxonomyContentsAdminNode treeNode)
        {
            return Initialize<TaxonomyContentsAdminNodeViewModel>("TaxonomyContentsAdminNode_Fields_TreeEdit", model =>
            {
                model.TaxonomyContentItemId = treeNode.TaxonomyContentItemId;
                model.IconForTree = treeNode.IconForTree;
                model.TaxonomyDisplayPattern = treeNode.TaxonomyDisplayPattern;
                model.ContentItemDisplayPattern = treeNode.ContentItemDisplayPattern;
            }).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(TaxonomyContentsAdminNode treeNode, IUpdateModel updater)
        {
            var model = new TaxonomyContentsAdminNodeViewModel();

            if (await updater.TryUpdateModelAsync(model, Prefix,
                x => x.TaxonomyContentItemId,
                x => x.IconForTree,
                x => x.TaxonomyDisplayPattern,
                x => x.ContentItemDisplayPattern))
            {
                treeNode.TaxonomyContentItemId = model.TaxonomyContentItemId;
                treeNode.IconForTree = model.IconForTree;
                treeNode.TaxonomyDisplayPattern = model.TaxonomyDisplayPattern;
                treeNode.ContentItemDisplayPattern = model.ContentItemDisplayPattern;
            };

            return Edit(treeNode);
        }
    }
}
