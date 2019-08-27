using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using OrchardCore.Navigation;
using OrchardCore.Settings;
using YesSql;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class UrlTreeAdminNodeNavigationBuilder : IAdminNodeNavigationBuilder
    {
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly IContentManager _contentManager;
        private readonly YesSql.ISession _session;
        private readonly ILogger<UrlTreeAdminNodeNavigationBuilder> _logger;
        private UrlTreeAdminNode _node;
        //private ContentTypeDefinition _contentType;

        private readonly IHttpContextAccessor _httpContextAccessor;
        //TODO implement
        private const int MaxItemsInNode = 100; // security check

        private readonly AutorouteOptions _options;
        private readonly IStringLocalizer<UrlTreeAdminNodeNavigationBuilder> T;
        public UrlTreeAdminNodeNavigationBuilder(
            IHttpContextAccessor httpContextAccessor,
            IContentDefinitionManager contentDefinitionManager,
            IContentManager contentManager,
            YesSql.ISession session,
            IOptions<AutorouteOptions> options,
            IStringLocalizer<UrlTreeAdminNodeNavigationBuilder> stringLocalizer,
            ILogger<UrlTreeAdminNodeNavigationBuilder> logger)
        {
            _contentDefinitionManager = contentDefinitionManager;
            _contentManager = contentManager;
            _session = session;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            T = stringLocalizer;
        }

        public string Name => typeof(UrlTreeAdminNode).Name;

        public async Task BuildNavigationAsync(MenuItem menuItem, NavigationBuilder builder, IEnumerable<IAdminNodeNavigationBuilder> treeNodeBuilders)
        {
            _node = menuItem as UrlTreeAdminNode;

            if ((_node == null) || (!_node.Enabled))
            {
                return;
            }

            //TODO cache
            var contentItems = (await _session
                .Query<ContentItem>()
                .With<AutoroutePartIndex>(o => o.Published)
                .ListAsync()).ToList();

            // Dissect into paths

            // First get HomeRoute
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

            var homeRouteEntry = await _contentManager.GetAsync(homeRouteContentItemId);
            if (homeRouteEntry == null)
            {
                return;
            }

            var homeRouteMeta = await _contentManager.PopulateAspectAsync<ContentItemMetadata>(homeRouteEntry);
            if (!homeRouteMeta.AdminRouteValues.Any())
            {
                return;
            }
            homeRouteMeta.AdminRouteValues["Action"] = "Edit";
            var rootMenuText = homeRouteEntry.DisplayText ?? T["URL Tree"];

            // Split the child menu levels
            var levels = new List<Level>();
            foreach (var ci in contentItems)
            {
                var part = ci.As<AutoroutePart>();
                var url = Tuple.Create(part.Path.Split('/'), ci);
                BuildLevel(levels, url, contentItems);
            }

            await builder.AddAsync(new LocalizedString(rootMenuText, rootMenuText), async urlTreeRoot =>
            {
                urlTreeRoot.Action(homeRouteMeta.AdminRouteValues["Action"] as string, homeRouteMeta.AdminRouteValues["Controller"] as string, homeRouteMeta.AdminRouteValues);
                urlTreeRoot.Resource(homeRouteEntry);
                urlTreeRoot.Priority(_node.Priority);
                urlTreeRoot.Position(_node.Position);
                urlTreeRoot.LocalNav();
                AddPrefixToClasses(_node.IconForTree).ToList().ForEach(c => urlTreeRoot.AddClass(c));

                await BuildMenuLevels(urlTreeRoot, levels);
                //TODO permissions
                //m.Permission(ContentTypePermissions.CreateDynamicPermission(
                //ContentTypePermissions.PermissionTemplates[Contents.Permissions.EditContent.Name], _contentType));

                //}
            });
        }

        private async Task BuildMenuLevels(NavigationItemBuilder urlTreeRoot, List<Level> levels)
        {
            foreach (var level in levels)
            {
                var cim = await _contentManager.PopulateAspectAsync<ContentItemMetadata>(level.ContentItem);
                cim.AdminRouteValues["Action"] = "Edit";
                //var display = new LocalizedString(level.DisplayText, level.DisplayText);
                var display = new LocalizedString(level.Segment, level.Segment);
                await urlTreeRoot.AddAsync(display, display, async menuLevel =>
                {
                    menuLevel.Action(cim.AdminRouteValues["Action"] as string, cim.AdminRouteValues["Controller"] as string, cim.AdminRouteValues);
                    menuLevel.Resource(level.ContentItem);
                    await BuildMenuLevels(menuLevel, level.SubLevels);
                });

            }
        }

        private void BuildLevel(List<Level> levels, Tuple<string[], ContentItem> url, List<ContentItem> contentItems)
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
                        Segment = url.Item1[i],
                        ContentItem = url.Item2
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

                    BuildLevel(level.SubLevels, subLevelUrl, contentItems);
                    break;
                }

                // If this is the last one in the segment
                if (i == url.Item1.Length - 1)
                {
                    var lastCi = contentItems.FirstOrDefault(x => x.As<AutoroutePart>().Path.EndsWith(level.Segment));

                    level.ContentItem = lastCi;
                    level.DisplayText = lastCi.Content.DocItemPart?.SubTitle;
                    if (level.DisplayText == null)
                    {
                        level.DisplayText = lastCi.DisplayText;
                    }
                    level.DisplayText = lastCi.Content.DocItemPart?.SubTitle;
                    if (level.DisplayText == null)
                    {
                        level.DisplayText = lastCi.DisplayText;
                    }

                }
            }
        }

        public class Level
        {
            public int Index { get; set; }
            public string Segment { get; set; }
            public string DisplayText { get; set; }
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
