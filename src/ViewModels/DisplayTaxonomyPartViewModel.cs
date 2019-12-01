using System.Collections.Generic;
using System.Linq;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata.Models;
using OrchardCore.Taxonomies.Models;

namespace ThisNetWorks.OrchardCore.AdminTree.ViewModels
{
    public class DisplayTaxonomyPartViewModel
    {
        public string TermContentItemId { get; set; }
        public ContentItem TermContentItem { get; set; }
        public TaxonomyPart TaxonomyPart { get; set; }
        public IEnumerable<ContentItem> ContentItems { get; set; } = Enumerable.Empty<ContentItem>();
        public List<ContentTypeEntry> ContainedContentTypeDefinitions { get; set; } = new List<ContentTypeEntry>();
        public BuildPartDisplayContext Context { get; set; }
        public dynamic Pager { get; set; }
    }

    public class ContentTypeEntry
    {
        public ContentTypeDefinition ContentTypeDefinition { get; set; }
        public string TaxonomyFieldName { get; set; }
    }
}
