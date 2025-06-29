using Phyros.ConfigurationService.ConfigurationClient.Models;

namespace Phyros.ConfigurationService.ConfigurationClient;

public interface IClientInformationProvider
{
	Task<IEnumerable<string>> GetActiveClientNames();
	Task<IEnumerable<ClientInformationDto>> GetActiveClientInformation();
	Task<ClientInformationDto?> GetClientInformation(string clientName);
}
