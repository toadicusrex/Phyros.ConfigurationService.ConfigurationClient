namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

internal interface IConfigurationMetadataProvider
{
	bool HasManagedKey(string key);

}
