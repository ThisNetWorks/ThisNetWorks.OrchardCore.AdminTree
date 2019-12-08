using System;
using OrchardCore.ContentManagement;

namespace ThisNetworks.OrchardCore.AdminTree.Models
{
    public class TermContainerPart : ContentPart
    {
        //TODO this could also take an AllContentTypes flag, with a All, Except option.
        public string[] ContainedContentTypes { get; set; } = Array.Empty<string>();
        public bool Multiple { get; set; }
    }
}
