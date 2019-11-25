using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluid;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrchardCore.AdminMenu.Services;
using OrchardCore.Autoroute.Models;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Metadata;
using OrchardCore.ContentManagement.Records;
using OrchardCore.ContentManagement.Routing;
using OrchardCore.Contents.Security;
using OrchardCore.Liquid;
using OrchardCore.Navigation;
using OrchardCore.Settings;
using YesSql;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    //TODO Breaks if you have two of these.
    public class TaxonomyContentsAdminNodeNavigationBuilder : IAdminNodeNavigationBuilder
    {
        private readonly ISiteService _siteService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ILiquidTemplateManager _liquidTemplatemanager;
        private readonly IContentManager _contentManager;
        private readonly YesSql.ISession _session;
        private readonly ILogger _logger;

        //TODO implement and figure out how we might best handle
        private const int MaxItemsInNode = 100; // security check

        private readonly IStringLocalizer T;

        public TaxonomyContentsAdminNodeNavigationBuilder(
            ISiteService siteService,
            IHttpContextAccessor httpContextAccessor,
            IContentDefinitionManager contentDefinitionManager,
            ILiquidTemplateManager liquidTemplateManager,
            IContentManager contentManager,
            YesSql.ISession session,
            IStringLocalizer<TaxonomyContentsAdminNodeNavigationBuilder> stringLocalizer,
            ILogger<TaxonomyContentsAdminNodeNavigationBuilder> logger)
        {
            _siteService = siteService;
            _httpContextAccessor = httpContextAccessor;
            _contentDefinitionManager = contentDefinitionManager;
            _liquidTemplatemanager = liquidTemplateManager;
            _contentManager = contentManager;
            _session = session;
            _logger = logger;
            T = stringLocalizer;
        }

        public string Name => typeof(TaxonomyContentsAdminNode).Name;

        public async Task BuildNavigationAsync(MenuItem menuItem, NavigationBuilder builder, IEnumerable<IAdminNodeNavigationBuilder> treeNodeBuilders)
        {
            var node = menuItem as TaxonomyContentsAdminNode;

            if ((node == null) || (!node.Enabled))
            {
                return;
            }

            var taxonomy = await _contentManager.GetAsync(node.TaxonomyContentItemId, VersionOptions.Latest);

            if (taxonomy == null)
            {
                return;
            }


            var templateContext = new TemplateContext();
            templateContext.SetValue("ContentItem", taxonomy);

            var taxonomyDisplayText = await _liquidTemplatemanager.RenderAsync(node.TaxonomyDisplayPattern, NullEncoder.Default, templateContext);



            //TODO Dynamic string localization not supported yet.
            //await builder.AddAsync(new LocalizedString(taxonomyDisplayText, taxonomyDisplayText), async urlTreeRoot =>
            //{
            //    //var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(homeRouteContentItem.ContentType);
            //    //urlTreeRoot.Action(homeRouteMetadata.AdminRouteValues["Action"] as string, homeRouteMetadata.AdminRouteValues["Controller"] as string, homeRouteMetadata.AdminRouteValues);
            //    //urlTreeRoot.Resource(homeRouteContentItem);
            //    urlTreeRoot.Priority(node.Priority);
            //    urlTreeRoot.Position(node.Position);
            //    urlTreeRoot.LocalNav();
            //    AddPrefixToClasses(node.IconForTree).ToList().ForEach(c => urlTreeRoot.AddClass(c));

            //    urlTreeRoot.Permission(ContentTypePermissions.CreateDynamicPermission(
            //        ContentTypePermissions.PermissionTemplates[global::OrchardCore.Contents.Permissions.EditContent.Name], contentTypeDefinition));
            //    await BuildMenuLevels(urlTreeRoot, levels);
            //});
        }

        //private async Task BuildMenuLevels(NavigationItemBuilder urlTreeRoot, List<Level> levels)
        //{
        //    foreach (var level in levels)
        //    {
        //        ContentItemMetadata cim = null;
        //        // Not all segments will have a content item associated with them.
        //        if (level.ContentItem != null)
        //        {
        //            cim = await _contentManager.PopulateAspectAsync<ContentItemMetadata>(level.ContentItem);
        //        }
        //        // TODO fix for list, which by default uses display.
        //        if (cim != null)
        //        {
        //            cim.AdminRouteValues["Action"] = "Edit";
        //        }

        //        await urlTreeRoot.AddAsync(level.DisplayText, level.DisplayText, async menuLevel =>
        //        {
        //            if (level.ContentItem != null)
        //            {
        //                menuLevel.Action(cim.AdminRouteValues["Action"] as string, cim.AdminRouteValues["Controller"] as string, cim.AdminRouteValues);
        //                menuLevel.Resource(level.ContentItem);
        //                var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(level.ContentItem.ContentType);
        //                menuLevel.Permission(ContentTypePermissions.CreateDynamicPermission(
        //                    ContentTypePermissions.PermissionTemplates[global::OrchardCore.Contents.Permissions.EditContent.Name], contentTypeDefinition));
        //            }
        //            //menuLevel.Caption(T["test"]);
        //            await BuildMenuLevels(menuLevel, level.SubLevels);
        //        });
        //    }
        //}
        
        private List<string> AddPrefixToClasses(string unprefixed)
        {
            return unprefixed?.Split(' ')
                .ToList()
                .Select(c => "icon-class-" + c)
                .ToList<string>()
                ?? new List<string>();
        }
    }
}
