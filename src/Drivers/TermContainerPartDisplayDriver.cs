using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.Models;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.DisplayManagement.ModelBinding;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Taxonomies.Models;
using ThisNetworks.OrchardCore.AdminTree.Models;
using ThisNetworks.OrchardCore.AdminTree.ViewModels;

namespace ThisNetWorks.OrchardCore.AdminTree.Drivers
{
    public class TermContainerPartDisplayDriver : ContentPartDisplayDriver<TermContainerPart>
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IStringLocalizer S;
        public TermContainerPartDisplayDriver(
            IContentDefinitionManager contentDefinitionManager,
            IStringLocalizer<TermContainerPartDisplayDriver> stringLocalizer
            )
        {
            _contentDefinitionManager = contentDefinitionManager;
            S = stringLocalizer;
        }

        public override IDisplayResult Edit(TermContainerPart part, BuildPartEditorContext context)
        {
            return Initialize<TermContainerPartViewModel>("TermContainerPart", model =>
            {
                model.TermContainerPart = part;
                model.ContainedContentTypes = part.ContainedContentTypes;
                model.ContentTypes = new NameValueCollection();

                foreach (var contentTypeDefinition in _contentDefinitionManager.ListTypeDefinitions())
                {
                    model.ContentTypes.Add(contentTypeDefinition.Name, contentTypeDefinition.DisplayName);
                }
            });
        }

        public override async Task<IDisplayResult> UpdateAsync(TermContainerPart part, IUpdateModel updater, UpdatePartEditorContext context)
        {
            var viewModel = new TermContainerPartViewModel();

            if (await context.Updater.TryUpdateModelAsync(viewModel, Prefix, m => m.ContainedContentTypes))
            {

                if (viewModel.ContainedContentTypes == null || viewModel.ContainedContentTypes.Length == 0)
                {
                    context.Updater.ModelState.AddModelError(nameof(viewModel.ContainedContentTypes), S["At least one content type must be selected."]);
                }
                else
                {
                    part.ContainedContentTypes = viewModel.ContainedContentTypes;
                }
            }
            return Edit(part, context);
        }
    }
}
