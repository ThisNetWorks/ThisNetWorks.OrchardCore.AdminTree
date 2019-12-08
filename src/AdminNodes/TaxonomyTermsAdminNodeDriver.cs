using System.Threading.Tasks;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Navigation;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class TaxonomyTermsAdminNodeDriver : DisplayDriver<MenuItem, TaxonomyTermsAdminNode>
    {
        public override IDisplayResult Display(TaxonomyTermsAdminNode treeNode)
        {
            return Combine(
                View("TaxonomyTermsAdminNode_Fields_TreeSummary", treeNode).Location("TreeSummary", "Content"),
                View("TaxonomyTermsAdminNode_Fields_TreeThumbnail", treeNode).Location("TreeThumbnail", "Content")
            );
        }

        public override IDisplayResult Edit(TaxonomyTermsAdminNode treeNode)
        {
            return Initialize<TaxonomyTermsAdminNodeViewModel>("TaxonomyTermsAdminNode_Fields_TreeEdit", model =>
            {
                model.TaxonomyContentItemId = treeNode.TaxonomyContentItemId;
                model.IconForTree = treeNode.IconForTree;
                model.TaxonomyDisplayPattern = treeNode.TaxonomyDisplayPattern;
                model.TermDisplayPattern = treeNode.TermDisplayPattern;
            }).Location("Content");
        }

        public override async Task<IDisplayResult> UpdateAsync(TaxonomyTermsAdminNode treeNode, IUpdateModel updater)
        {
            var model = new TaxonomyTermsAdminNodeViewModel();

            if (await updater.TryUpdateModelAsync(model, Prefix,
                x => x.TaxonomyContentItemId,
                x => x.IconForTree,
                x => x.TaxonomyDisplayPattern,
                x => x.TermDisplayPattern))
            {
                treeNode.TaxonomyContentItemId = model.TaxonomyContentItemId;
                treeNode.IconForTree = model.IconForTree;
                treeNode.TaxonomyDisplayPattern = model.TaxonomyDisplayPattern;
                treeNode.TermDisplayPattern = model.TermDisplayPattern;
            };

            return Edit(treeNode);
        }
    }
}
