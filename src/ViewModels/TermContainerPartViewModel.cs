using Microsoft.AspNetCore.Mvc.ModelBinding;
using ThisNetworks.OrchardCore.AdminTree.Models;

namespace ThisNetworks.OrchardCore.AdminTree.ViewModels
{
    public class TermContainerPartViewModel
    {
        public bool AllContentTypes { get; set; }
        public ContentTypeEntryViewModel[] ContainedContentTypes { get; set; }
        public bool Multiple { get; set; }

        [BindNever]
        public TermContainerPart TermContainerPart { get; set; }
    }

    public class ContentTypeEntryViewModel
    {
        public bool IsSelected { get; set; }
        public string ContentTypeName { get; set; }
        public string ContentTypeDisplayName { get; set; }
    }

    public class TaxonomyPartIdViewModel
    {
        public string TaxonomyContentItemId { get; set; }
    }
}
