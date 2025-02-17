using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.DependencyInjection;

namespace Ocelot.Configuration.Creator
{
    public class ConfigurationCreator : IConfigurationCreator
    {
        private readonly IServiceProviderConfigurationCreator _serviceProviderConfigCreator;
        private readonly IQoSOptionsCreator _qosOptionsCreator;
        private readonly IHttpHandlerOptionsCreator _httpHandlerOptionsCreator;
        private readonly IAdministrationPath _adminPath;
        private readonly ILoadBalancerOptionsCreator _loadBalancerOptionsCreator;
        private readonly IVersionCreator _versionCreator;
        private readonly IVersionPolicyCreator _versionPolicyCreator;

        public ConfigurationCreator(
            IServiceProviderConfigurationCreator serviceProviderConfigCreator,
            IQoSOptionsCreator qosOptionsCreator,
            IHttpHandlerOptionsCreator httpHandlerOptionsCreator,
            IServiceProvider serviceProvider,
            ILoadBalancerOptionsCreator loadBalancerOptionsCreator,
            IVersionCreator versionCreator,
            IVersionPolicyCreator versionPolicyCreator
            )
        {
            _adminPath = serviceProvider.GetService<IAdministrationPath>();
            _loadBalancerOptionsCreator = loadBalancerOptionsCreator;
            _serviceProviderConfigCreator = serviceProviderConfigCreator;
            _qosOptionsCreator = qosOptionsCreator;
            _httpHandlerOptionsCreator = httpHandlerOptionsCreator;
            _versionCreator = versionCreator;
            _versionPolicyCreator = versionPolicyCreator;
        }

        public InternalConfiguration Create(FileConfiguration fileConfiguration, List<Route> routes)
        {
            var serviceProviderConfiguration = _serviceProviderConfigCreator.Create(fileConfiguration.GlobalConfiguration);

            var lbOptions = _loadBalancerOptionsCreator.Create(fileConfiguration.GlobalConfiguration.LoadBalancerOptions);

            var qosOptions = _qosOptionsCreator.Create(fileConfiguration.GlobalConfiguration.QoSOptions);

            var httpHandlerOptions = _httpHandlerOptionsCreator.Create(fileConfiguration.GlobalConfiguration.HttpHandlerOptions);

            var adminPath = _adminPath?.Path;

            var version = _versionCreator.Create(fileConfiguration.GlobalConfiguration.DownstreamHttpVersion);

            var versionPolicy = _versionPolicyCreator.Create(fileConfiguration.GlobalConfiguration.DownstreamHttpVersionPolicy);

            return new InternalConfiguration(routes,
                adminPath,
                serviceProviderConfiguration,
                fileConfiguration.GlobalConfiguration.RequestIdKey,
                lbOptions,
                fileConfiguration.GlobalConfiguration.DownstreamScheme,
                qosOptions,
                httpHandlerOptions,
                version,
                versionPolicy
                );
        }
    }
}
