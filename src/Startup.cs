using Microsoft.Extensions.DependencyInjection;
using OrchardCore.AdminMenu.Services;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using ThisNetWorks.OrchardCore.AdminTree.AdminNodes;
using ThisNetWorks.OrchardCore.AdminTree.Drivers;

namespace ThisNetworks.OrchardCore.AdminTree
{
    [RequireFeatures("OrchardCore.AdminMenu")]
    public class Startup : StartupBase
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAdminNodeProviderFactory>(new AdminNodeProviderFactory<UrlTreeAdminNode>());
            services.AddScoped<IAdminNodeNavigationBuilder, UrlTreeAdminNodeNavigationBuilder>();
            services.AddScoped<IDisplayDriver<MenuItem>, UrlTreeAdminNodeDriver>();


            services.AddSingleton<IAdminNodeProviderFactory>(new AdminNodeProviderFactory<TaxonomyTermsAdminNode>());
            services.AddScoped<IAdminNodeNavigationBuilder, TaxonomyTermsAdminNodeNavigationBuilder>();
            services.AddScoped<IDisplayDriver<MenuItem>, TaxonomyTermsAdminNodeDriver>();
            services.AddScoped<IContentPartDisplayDriver, TaxonomyPartDisplayDriver>();


            services.AddSingleton<IAdminNodeProviderFactory>(new AdminNodeProviderFactory<TaxonomyContentsAdminNode>());
            services.AddScoped<IAdminNodeNavigationBuilder, TaxonomyContentsAdminNodeNavigationBuilder>();
            services.AddScoped<IDisplayDriver<MenuItem>, TaxonomyContentsAdminNodeDriver>();
        }
    }
}
