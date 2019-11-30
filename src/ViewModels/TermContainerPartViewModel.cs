using System.Collections.Specialized;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ThisNetworks.OrchardCore.AdminTree.Models;

namespace ThisNetworks.OrchardCore.AdminTree.ViewModels
{
    public class TermContainerPartViewModel
    {
        public NameValueCollection ContentTypes { get; set; }
        public string[] ContainedContentTypes { get; set; }

        [BindNever]
        public TermContainerPart TermContainerPart { get; set; }
    }
}
