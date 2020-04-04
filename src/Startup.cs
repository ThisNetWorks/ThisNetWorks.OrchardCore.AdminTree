using System;
using Microsoft.Extensions.DependencyInjection;
using OrchardCore.AdminMenu.Services;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.Data.Migration;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using OrchardCore.Taxonomies.Drivers;
using OrchardCore.Taxonomies.Fields;
using OrchardCore.Taxonomies.Models;
using ThisNetworks.OrchardCore.AdminTree.Models;
using ThisNetWorks.OrchardCore.AdminTree.AdminNodes;
using ThisNetWorks.OrchardCore.AdminTree.Drivers;

namespace ThisNetworks.OrchardCore.AdminTree
{
    [RequireFeatures("OrchardCore.AdminMenu")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            // UrlTree
            services.AddSingleton<IAdminNodeProviderFactory>(new AdminNodeProviderFactory<UrlTreeAdminNode>());
            services.AddScoped<IAdminNodeNavigationBuilder, UrlTreeAdminNodeNavigationBuilder>();
            services.AddScoped<IDisplayDriver<MenuItem>, UrlTreeAdminNodeDriver>();

            // TaxonomyTerms Menu
            services.AddSingleton<IAdminNodeProviderFactory>(new AdminNodeProviderFactory<TaxonomyTermsAdminNode>());
            services.AddScoped<IAdminNodeNavigationBuilder, TaxonomyTermsAdminNodeNavigationBuilder>();
            services.AddScoped<IDisplayDriver<MenuItem>, TaxonomyTermsAdminNodeDriver>();

            services.AddContentPart<TaxonomyPart>()
                .UseDisplayDriver<TaxonomyPartAdminDisplayDriver>();

            //services.AddSingleton<IAdminNodeProviderFactory>(new AdminNodeProviderFactory<TaxonomyContentsAdminNode>());
            //services.AddScoped<IAdminNodeNavigationBuilder, TaxonomyContentsAdminNodeNavigationBuilder>();
            //services.AddScoped<IDisplayDriver<MenuItem>, TaxonomyContentsAdminNodeDriver>();

            // Term container part
            services.AddContentPart<TermContainerPart>()
                .UseDisplayDriver<TermContainerPartDisplayDriver>();

            services.AddScoped<IDataMigration, Migrations>();

            services.AddContentField<TaxonomyField>()
                .UseDisplayDriver<TaxonomyFieldDisplayDriver>(d => !String.Equals(d, "Tags", StringComparison.OrdinalIgnoreCase) &&
                !String.Equals(d, "Contained", StringComparison.OrdinalIgnoreCase));

            services.AddContentField<TaxonomyField>()
                .UseDisplayDriver<TaxonomyFieldContainedDisplayDriver>(d => String.Equals(d, "Contained", StringComparison.OrdinalIgnoreCase));

        }
    }
}
