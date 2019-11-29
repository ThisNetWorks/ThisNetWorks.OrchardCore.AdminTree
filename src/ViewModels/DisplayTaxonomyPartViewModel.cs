using System.Collections.Generic;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.Taxonomies.Models;

namespace ThisNetWorks.OrchardCore.AdminTree.ViewModels
{
    public class DisplayTaxonomyPartViewModel
    {
        public string TermContentItemId { get; set; }
        public TermPart TermPart { get; set; }
        public TaxonomyPart TaxonomyPart { get; set; }
        public List<ContentItem> ContentItems { get; set; } = new List<ContentItem>();
        public BuildPartDisplayContext Context { get; set; }
        public dynamic Pager { get; set; }
    }
}
