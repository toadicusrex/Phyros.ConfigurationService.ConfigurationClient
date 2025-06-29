using Microsoft.Extensions.Configuration;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
internal class PhyrosConfigurationSource(PhyrosConfigurationProvider configurationProvider) : IConfigurationSource
{
	public IConfigurationProvider Build(IConfigurationBuilder builder) => configurationProvider;
}
