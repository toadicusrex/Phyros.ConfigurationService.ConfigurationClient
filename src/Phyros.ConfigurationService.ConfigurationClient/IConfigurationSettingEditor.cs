using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.Models;

namespace Phyros.ConfigurationService.ConfigurationClient;
public interface IConfigurationApiValueWriter
{
	Task SetValue(ConfigurationKey configurationKey, PhyrosConfigurationSetting value);
}
