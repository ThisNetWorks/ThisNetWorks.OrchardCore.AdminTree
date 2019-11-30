using OrchardCore.ContentManagement;

namespace ThisNetworks.OrchardCore.AdminTree.Models
{
    public class TermContainerPart : ContentPart
    {
        public string[] ContainedContentTypes { get; set; }
    }
}
