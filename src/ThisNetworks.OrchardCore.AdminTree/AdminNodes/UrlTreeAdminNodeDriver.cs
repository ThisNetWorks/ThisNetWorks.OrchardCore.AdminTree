using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Navigation;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class UrlTreeAdminNodeDriver : DisplayDriver<MenuItem, UrlTreeAdminNode>
    {
        public override IDisplayResult Display(UrlTreeAdminNode treeNode)
        {
            return Combine(
                View("UrlTreeAdminNode_Fields_TreeSummary", treeNode).Location("TreeSummary", "Content"),
                View("UrlTreeAdminNode_Fields_TreeThumbnail", treeNode).Location("TreeThumbnail", "Content")
            );
        }

        public override IDisplayResult Edit(UrlTreeAdminNode treeNode)
        {
            return Initialize<UrlTreeAdminNodeViewModel>("UrlTreeAdminNode_Fields_TreeEdit", model =>
            {
                model.IconForTree = treeNode.IconForTree;
                model.TreeRootDisplayPattern = treeNode.TreeRootDisplayPattern;
                model.UseItemSegmentForDisplay = treeNode.UseItemSegmentForDisplay;
                model.ItemDisplayPattern = treeNode.ItemDisplayPattern;
            }).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(UrlTreeAdminNode treeNode, IUpdateModel updater)
        {
            var model = new UrlTreeAdminNodeViewModel();

            if (await updater.TryUpdateModelAsync(model, Prefix,
                x => x.IconForTree,
                x => x.TreeRootDisplayPattern,
                x => x.UseItemSegmentForDisplay,
                x => x.ItemDisplayPattern))
            {
                treeNode.IconForTree = model.IconForTree;
                treeNode.TreeRootDisplayPattern = model.TreeRootDisplayPattern;
                treeNode.UseItemSegmentForDisplay = model.UseItemSegmentForDisplay;
                treeNode.ItemDisplayPattern = model.ItemDisplayPattern;
            };

            return Edit(treeNode);
        }
    }
}
