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
    // Could be address by having a start segment.
    // So that you could select a part of the sites url structure to start at.
    // That would also resolve the MaxItems question.
    public class UrlTreeAdminNodeNavigationBuilder : IAdminNodeNavigationBuilder
    {
        private readonly ISiteService _siteService;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ILiquidTemplateManager _liquidTemplatemanager;
        private readonly IContentManager _contentManager;
        private readonly YesSql.ISession _session;
        private readonly ILogger<UrlTreeAdminNodeNavigationBuilder> _logger;

        //TODO implement and figure out how we might best handle
        private const int MaxItemsInNode = 100; // security check

        private readonly AutorouteOptions _options;
        public UrlTreeAdminNodeNavigationBuilder(
            ISiteService siteService,
            IContentDefinitionManager contentDefinitionManager,
            ILiquidTemplateManager liquidTemplateManager,
            IContentManager contentManager,
            YesSql.ISession session,
            IOptions<AutorouteOptions> options,
            ILogger<UrlTreeAdminNodeNavigationBuilder> logger)
        {
            _siteService = siteService;
            _contentDefinitionManager = contentDefinitionManager;
            _liquidTemplatemanager = liquidTemplateManager;
            _contentManager = contentManager;
            _session = session;
            _logger = logger;
            _options = options.Value;
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


            var homeRoute = (await _siteService.GetSiteSettingsAsync()).HomeRoute;

            // Return on no homeroute.
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

            var rootMenuDisplayText = await _liquidTemplatemanager.RenderAsync(node.TreeRootDisplayPattern, NullEncoder.Default, templateContext);
            // In case of bad liquid.
            if (String.IsNullOrEmpty(rootMenuDisplayText))
            {
                _logger.LogError("Bad liquid root menu display text");
            }
            var segments = new List<ContentItemSegment2>();
            foreach (var ci in contentItems)
            {
                var part = ci.As<AutoroutePart>();
                var contentItemSegment = new ContentItemSegment2
                {
                    Segments = part.Path.Contains("/") ? part.Path.Split('/') : new string[] { part.Path },
                    Path = part.Path,
                    ContentItem = ci
                };
                segments.Add(contentItemSegment);
            }

            var levels = new List<Level>();
            await BuildLevels(levels, segments, node, 0);

            //TODO Dynamic string localization not supported yet.
            await builder.AddAsync(new LocalizedString(rootMenuDisplayText, rootMenuDisplayText), async urlTreeRoot =>
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
                // TODO fix for list, which by default uses display. Hmm is this fixed?
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

        private async Task BuildLevels(List<Level> levels, List<ContentItemSegment2> contentItemSegments, UrlTreeAdminNode node, int index)
        {
            // Materialize this.
            var segments = contentItemSegments
                .Where(x => x.Segments.Length > 0)
                .Select(x => x.Segments[0]).Distinct()
                .ToList();

            foreach (var segment in segments)
            {
                // This may return null, as some path segments may not have a content item.
                var currentSegment = contentItemSegments
                    .FirstOrDefault(x => x.Segments.Length == 1 && x.Segments[0] == segment);

                var level = new Level()
                {
                    Index = index,
                    Segment = segment,
                    Path = currentSegment?.Path,
                    ContentItem = currentSegment?.ContentItem
                };
                levels.Add(level);

                if (node.UseItemSegmentForDisplay || level.ContentItem == null)
                {
                    // TODO This should be in css. but flex box / time.
                    var truncated = level.Segment.Truncate(17);
                    level.DisplayText = new LocalizedString(truncated, truncated);
                }
                else
                {
                    var templateContext = new TemplateContext();
                    templateContext.SetValue("ContentItem", level.ContentItem);

                    var displayText = await _liquidTemplatemanager.RenderAsync(node.ItemDisplayPattern, NullEncoder.Default, templateContext);
                    // In case of bad liquid.
                    if (String.IsNullOrEmpty(displayText))
                    {
                        _logger.LogError("Bad liquid setting display text for segment {Segment} on path {Path}", level.Segment, level.Path);

                        var truncated = level.Segment.Truncate(17);
                        level.DisplayText = new LocalizedString(truncated, truncated);
                    }
                    else
                    {
                        displayText = level.Segment.Truncate(17);
                        level.DisplayText = new LocalizedString(displayText, displayText);
                    }
                }

                var children = contentItemSegments.Where(x => x != currentSegment &&
                    x.Segments.Length > 0 && x.Segments[0] == segment).ToList();

                if (children.Count > 0)
                {
                    foreach (var child in children)
                    {
                        child.Segments = child.Segments.Skip(1).ToArray();
                    }
                    await BuildLevels(level.SubLevels, children, node, index + 1);
                }
            }
        }

        public class ContentItemSegments
        {
            public string[] Segments { get; set; }
            public string Path { get; set; }
            public ContentItem ContentItem { get; set; }
        }

        public class Level
        {
            public int Index { get; set; }
            public string Segment { get; set; }
            public string Path { get; set; }
            public LocalizedString DisplayText { get; set; }
            public ContentItem ContentItem { get; set; }

            public List<Level> SubLevels { get; set; } = new List<Level>();
        }

        public class ContentItemSegment2
        {
            public string[] Segments { get; set; }
            public string Path { get; set; }
            public ContentItem ContentItem { get; set; }
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

    public static class StringExtensions{
        public static string Truncate(this string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
    }
}
