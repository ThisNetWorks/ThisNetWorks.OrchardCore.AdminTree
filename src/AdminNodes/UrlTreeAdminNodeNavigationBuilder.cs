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
using OrchardCore.Autoroute.Model;
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
    public class UrlTreeAdminNodeNavigationBuilder : IAdminNodeNavigationBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ILiquidTemplateManager _liquidTemplatemanager;
        private readonly IContentManager _contentManager;
        private readonly YesSql.ISession _session;
        private readonly ILogger<UrlTreeAdminNodeNavigationBuilder> _logger;

        //TODO implement and figure out how we might best handle
        private const int MaxItemsInNode = 100; // security check

        private readonly AutorouteOptions _options;
        private readonly IStringLocalizer<UrlTreeAdminNodeNavigationBuilder> T;
        public UrlTreeAdminNodeNavigationBuilder(
            IHttpContextAccessor httpContextAccessor,
            IContentDefinitionManager contentDefinitionManager,
            ILiquidTemplateManager liquidTemplateManager,
            IContentManager contentManager,
            YesSql.ISession session,
            IOptions<AutorouteOptions> options,
            IStringLocalizer<UrlTreeAdminNodeNavigationBuilder> stringLocalizer,
            ILogger<UrlTreeAdminNodeNavigationBuilder> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _contentDefinitionManager = contentDefinitionManager;
            _liquidTemplatemanager = liquidTemplateManager;
            _contentManager = contentManager;
            _session = session;
            _logger = logger;
            _options = options.Value;
            T = stringLocalizer;
        }

        public string Name => typeof(UrlTreeAdminNode).Name;

        public async Task BuildNavigationAsync(MenuItem menuItem, NavigationBuilder builder, IEnumerable<IAdminNodeNavigationBuilder> treeNodeBuilders)
        {
            var node = menuItem as UrlTreeAdminNode;

            if ((node == null) || (!node.Enabled))
            {
                return;
            }

            //TODO cache (actually cache the levels.)
            var contentItems = (await _session
                .Query<ContentItem>()
                .With<AutoroutePartIndex>(o => o.Published)
                .ListAsync()).ToList();

            var homeRoute = _httpContextAccessor.HttpContext.Features.Get<HomeRouteFeature>()?.HomeRoute;

            // Return on no homeroute (initially)
            if (homeRoute == null)
            {
                return;
            }

            var homeRouteContentItemId = homeRoute[_options.ContentItemIdKey]?.ToString();
            if (String.IsNullOrEmpty(homeRouteContentItemId))
            {
                return;
            }

            var homeRouteContentItem = await _contentManager.GetAsync(homeRouteContentItemId);
            if (homeRouteContentItem == null)
            {
                return;
            }

            var homeRouteMetadata = await _contentManager.PopulateAspectAsync<ContentItemMetadata>(homeRouteContentItem);
            if (!homeRouteMetadata.AdminRouteValues.Any())
            {
                return;
            }
            // In case of lists, which use display
            homeRouteMetadata.AdminRouteValues["Action"] = "Edit";

            var templateContext = new TemplateContext();
            templateContext.SetValue("ContentItem", homeRouteContentItem);

            var rootMenuText = await _liquidTemplatemanager.RenderAsync(node.TreeRootDisplayPattern, NullEncoder.Default, templateContext);

            // TODO Cache the levels, not the ContentItems.
            // Split the child menu levels
            var levels = new List<Level>();
            foreach (var ci in contentItems)
            {
                var part = ci.As<AutoroutePart>();
                var url = Tuple.Create(part.Path.Split('/'), ci);
                await BuildLevelAsync(levels, url, contentItems, node);
            }

            //TODO Dynamic string localization not supported yet.
            await builder.AddAsync(new LocalizedString(rootMenuText, rootMenuText), async urlTreeRoot =>
            {
                var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(homeRouteContentItem.ContentType);
                urlTreeRoot.Action(homeRouteMetadata.AdminRouteValues["Action"] as string, homeRouteMetadata.AdminRouteValues["Controller"] as string, homeRouteMetadata.AdminRouteValues);
                urlTreeRoot.Resource(homeRouteContentItem);
                urlTreeRoot.Priority(node.Priority);
                urlTreeRoot.Position(node.Position);
                urlTreeRoot.LocalNav();
                AddPrefixToClasses(node.IconForTree).ToList().ForEach(c => urlTreeRoot.AddClass(c));

                urlTreeRoot.Permission(ContentTypePermissions.CreateDynamicPermission(
                    ContentTypePermissions.PermissionTemplates[global::OrchardCore.Contents.Permissions.EditContent.Name], contentTypeDefinition));
                await BuildMenuLevels(urlTreeRoot, levels);
            });
        }

        private async Task BuildMenuLevels(NavigationItemBuilder urlTreeRoot, List<Level> levels)
        {
            foreach (var level in levels)
            {
                ContentItemMetadata cim = null;
                // Not all segments will have a content item associated with them.
                if (level.ContentItem != null)
                {
                    cim = await _contentManager.PopulateAspectAsync<ContentItemMetadata>(level.ContentItem);
                }
                // TODO fix for list, which by default uses display.
                if (cim != null)
                {
                    cim.AdminRouteValues["Action"] = "Edit";
                }

                await urlTreeRoot.AddAsync(level.DisplayText, level.DisplayText, async menuLevel =>
                {
                    if (level.ContentItem != null)
                    {
                        menuLevel.Action(cim.AdminRouteValues["Action"] as string, cim.AdminRouteValues["Controller"] as string, cim.AdminRouteValues);
                        menuLevel.Resource(level.ContentItem);
                        var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(level.ContentItem.ContentType);
                        menuLevel.Permission(ContentTypePermissions.CreateDynamicPermission(
                            ContentTypePermissions.PermissionTemplates[global::OrchardCore.Contents.Permissions.EditContent.Name], contentTypeDefinition));
                    }
                    await BuildMenuLevels(menuLevel, level.SubLevels);
                });
            }
        }

        private async Task BuildLevelAsync(List<Level> levels, Tuple<string[], ContentItem> url, List<ContentItem> contentItems, UrlTreeAdminNode node)
        {
            _logger.LogDebug("Parsing url {Url}", url.Item1);
            Level level = null;
            for (var i = 0; i < url.Item1.Length; i++)
            {
                // Assign
                if (level == null)
                {
                    level = levels.FirstOrDefault(x => x.Index == i && x.Segment == url.Item1[i]);
                }

                // If not there already, add it.
                if (level == null)
                {
                    level = new Level()
                    {
                        Index = i,
                        Segment = url.Item1[i]
                    };

                    _logger.LogDebug("Adding level index {Index}, Segment {Segment}, DisplayText {DisplayText}", level.Index, level.Segment, url.Item2.DisplayText);
                    levels.Add(level);
                }

                // If there is a next level, create it.
                if (i > level.Index)
                {
                    var newArray = new string[url.Item1.Length - i];
                    Array.Copy(url.Item1, i, newArray, 0, url.Item1.Length - i);
                    var subLevelUrl = Tuple.Create(newArray, url.Item2);

                    await BuildLevelAsync(level.SubLevels, subLevelUrl, contentItems, node);
                    break;
                }

                // If this is the last one in the segment
                if (i == url.Item1.Length - 1)
                {
                    var lastCi = contentItems.FirstOrDefault(x => x.As<AutoroutePart>().Path.EndsWith(level.Segment));

                    level.ContentItem = lastCi;
                    if (node.UseItemSegmentForDisplay || level.ContentItem == null)
                    {
                        level.DisplayText = new LocalizedString(level.Segment, level.Segment);
                    }
                    else
                    {
                        var templateContext = new TemplateContext();
                        templateContext.SetValue("ContentItem", level.ContentItem);

                        var display = await _liquidTemplatemanager.RenderAsync(node.ItemDisplayPattern, NullEncoder.Default, templateContext);
                        // In case of bad liquid
                        if (display == null)
                        {
                            level.DisplayText = new LocalizedString(level.Segment, level.Segment);
                        }
                        else
                        {
                            level.DisplayText = new LocalizedString(display, display);
                        }
                    }
                }
            }
        }

        public class Level
        {
            public int Index { get; set; }
            public string Segment { get; set; }
            public LocalizedString DisplayText { get; set; }
            public ContentItem ContentItem { get; set; }

            public List<Level> SubLevels { get; set; } = new List<Level>();
        }

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
