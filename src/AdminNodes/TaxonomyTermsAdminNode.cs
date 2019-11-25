using OrchardCore.AdminMenu.Models;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class TaxonomyTermsAdminNode : AdminNode
    {
        public string TaxonomyContentItemId { get; set; }
        public string IconForTree { get; set; }
        public string TaxonomyDisplayPattern { get; set; } = "{{ ContentItem | display_text }}";
        public string TermDisplayPattern { get; set; } = "{{ ContentItem | display_text }}";
    }
}
