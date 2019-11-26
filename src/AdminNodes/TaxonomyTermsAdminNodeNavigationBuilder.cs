using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
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
using OrchardCore.Taxonomies.Models;
using YesSql;

namespace ThisNetWorks.OrchardCore.AdminTree.AdminNodes
{
    public class TaxonomyTermsAdminNodeNavigationBuilder : IAdminNodeNavigationBuilder
    {
        private readonly ISiteService _siteService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ILiquidTemplateManager _liquidTemplatemanager;
        private readonly IContentManager _contentManager;
        private readonly YesSql.ISession _session;
        private readonly ILogger _logger;

        private const int MaxItemsInNode = 100; // security check

        private readonly IStringLocalizer T;

        public TaxonomyTermsAdminNodeNavigationBuilder(
            ISiteService siteService,
            IHttpContextAccessor httpContextAccessor,
            IContentDefinitionManager contentDefinitionManager,
            ILiquidTemplateManager liquidTemplateManager,
            IContentManager contentManager,
            YesSql.ISession session,
            IStringLocalizer<TaxonomyTermsAdminNodeNavigationBuilder> stringLocalizer,
            ILogger<TaxonomyTermsAdminNodeNavigationBuilder> logger
            )
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

        public string Name => typeof(TaxonomyTermsAdminNode).Name;

        public async Task BuildNavigationAsync(MenuItem menuItem, NavigationBuilder builder, IEnumerable<IAdminNodeNavigationBuilder> treeNodeBuilders)
        {
            var node = menuItem as TaxonomyTermsAdminNode;

            if ((node == null) || (!node.Enabled))
            {
                return;
            }
            
            var taxonomy = await _contentManager.GetAsync(node.TaxonomyContentItemId, VersionOptions.Latest);

            if (taxonomy == null)
            {
                return;
            }

            var homeRoute = (await _siteService.GetSiteSettingsAsync()).HomeRoute;
            var templateContext = new TemplateContext();
            templateContext.SetValue("ContentItem", taxonomy);

            var taxonomyDisplayText = await _liquidTemplatemanager.RenderAsync(node.TaxonomyDisplayPattern, NullEncoder.Default, templateContext);

            var termEntries = new List<TermMenuEntry>();
            PopulateTermEntries(termEntries, taxonomy.As<TaxonomyPart>().Terms, 0);

            await builder.AddAsync(new LocalizedString(taxonomyDisplayText, taxonomyDisplayText), async taxonomyRoot =>
            {
                //var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(homeRouteContentItem.ContentType);
                taxonomyRoot.Action(homeRoute["Action"] as string, homeRoute["Controller"] as string, homeRoute);
                //urlTreeRoot.Resource(homeRouteContentItem);
                taxonomyRoot.Priority(node.Priority);
                taxonomyRoot.Position(node.Position);
                taxonomyRoot.LocalNav();
                AddPrefixToClasses(node.IconForTree).ToList().ForEach(c => taxonomyRoot.AddClass(c));

                //taxonomyRoot.Permission(ContentTypePermissions.CreateDynamicPermission(
                //    ContentTypePermissions.PermissionTemplates[global::OrchardCore.Contents.Permissions.EditContent.Name], contentTypeDefinition));
                await BuildMenuLevels(taxonomyRoot, termEntries, 0, homeRoute);
            });
        }

        private async Task BuildMenuLevels(NavigationItemBuilder urlTreeRoot, List<TermMenuEntry> termMenuEntries, int level, RouteValueDictionary homeRoute, TermMenuEntry parent = null)
        {
            foreach (var termMenuEntry in termMenuEntries.Where(x => x.Level == level))
            {
                if (parent != null && termMenuEntry.Parent != parent)
                {
                    continue;
                }

                //ContentItemMetadata cim = null;
                // Not all segments will have a content item associated with them.
                //if (termMenuEntry.ContentItem != null)
                //{
                //    cim = await _contentManager.PopulateAspectAsync<ContentItemMetadata>(termMenuEntry.ContentItem);
                //}
                //// TODO fix for list, which by default uses display.
                //if (cim != null)
                //{
                //    cim.AdminRouteValues["Action"] = "Edit";
                //}

                var localizedDisplayText = new LocalizedString(termMenuEntry.Term.DisplayText, termMenuEntry.Term.DisplayText);

                await urlTreeRoot.AddAsync(localizedDisplayText, async menuLevel =>
                {
                    if (termMenuEntry.Term != null)
                    {
                        menuLevel.Action(homeRoute["Action"] as string, homeRoute["Controller"] as string, homeRoute);
                        //menuLevel.Action(cim.AdminRouteValues["Action"] as string, cim.AdminRouteValues["Controller"] as string, cim.AdminRouteValues);
                        //menuLevel.Resource(termMenuEntry.ContentItem);
                        //var contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(termMenuEntry.ContentItem.ContentType);
                        //menuLevel.Permission(ContentTypePermissions.CreateDynamicPermission(
                        //    ContentTypePermissions.PermissionTemplates[global::OrchardCore.Contents.Permissions.EditContent.Name], contentTypeDefinition));
                    }

                    //menuLevel.Caption(T["test"]);
                    await BuildMenuLevels(menuLevel, termMenuEntries, level + 1, homeRoute, termMenuEntry);
                });
            }
        }

        private void PopulateTermEntries(List<TermMenuEntry> termEntries, IEnumerable<ContentItem> contentItems, int level, TermMenuEntry parent = null)
        {
            foreach (var contentItem in contentItems)
            {
                var children = Array.Empty<ContentItem>();

                if (contentItem.Content.Terms is JArray termsArray)
                {
                    children = termsArray.ToObject<ContentItem[]>();
                }

                var termEntry = new TermMenuEntry
                {
                    Term = contentItem,
                    Parent = parent,
                    Level = level,
                    IsLeaf = children.Length == 0
                };

                termEntries.Add(termEntry);

                if (children.Length > 0)
                {
                    PopulateTermEntries(termEntries, children, level + 1, termEntry);
                }
            }
        }

        private List<string> AddPrefixToClasses(string unprefixed)
        {
            return unprefixed?.Split(' ')
                .ToList()
                .Select(c => "icon-class-" + c)
                .ToList<string>()
                ?? new List<string>();
        }

        public class TermMenuEntry
        {
            public ContentItem Term { get; set; }
            public TermMenuEntry Parent { get; set; }
            public int Level { get; set; }
            public bool IsLeaf { get; set; }
        }
    }
}
