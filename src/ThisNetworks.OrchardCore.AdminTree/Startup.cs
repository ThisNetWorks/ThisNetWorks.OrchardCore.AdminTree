using Microsoft.Extensions.DependencyInjection;
using OrchardCore.AdminMenu.Services;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.Modules;
using OrchardCore.Navigation;
using ThisNetWorks.OrchardCore.AdminTree.AdminNodes;

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
        }
    }
}
