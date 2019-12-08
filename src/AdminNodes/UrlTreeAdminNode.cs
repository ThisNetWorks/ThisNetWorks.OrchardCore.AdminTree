using OrchardCore.AdminMenu.Models;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class UrlTreeAdminNode : AdminNode
    {
        //TODO maybe a start segment?
        public string IconForTree { get; set; }
        public string TreeRootDisplayPattern { get; set; } = "{{ ContentItem | display_text }}";
        public bool UseItemSegmentForDisplay { get; set; }
        public string ItemDisplayPattern { get; set; } = "{{ ContentItem | display_text }}";
    }
}
