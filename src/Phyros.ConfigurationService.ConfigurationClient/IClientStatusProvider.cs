using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient;
public interface IClientStatusProvider
{
	Task<bool> IsActive(OrganizationalUnit organizationalUnit);
}
