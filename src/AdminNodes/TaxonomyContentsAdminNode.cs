using OrchardCore.AdminMenu.Models;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class TaxonomyContentsAdminNode : AdminNode
    {
        public string TaxonomyContentItemId { get; set; }
        public string IconForTree { get; set; }
        public string TaxonomyDisplayPattern { get; set; } = "{{ ContentItem | display_text }}";
        public string ContentItemDisplayPattern { get; set; } = "{{ ContentItem | display_text }}";
    }
}
