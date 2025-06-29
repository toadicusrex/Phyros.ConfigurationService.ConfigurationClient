using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

public class ConfigurationKey
{
	public ConfigurationKey(string simpleOrCompositeName, OrganizationalUnit defaultOrganizationalUnit)
	{
		// if the simpleOrCompositeName parameter contains an organizational unit, the default organizational unit parameter will be ignored.
		if (!simpleOrCompositeName.Contains(Models.Constants.ORGANIZATIONAL_UNIT_AND_KEY_DELIMITER))
		{
			OrganizationalUnit = defaultOrganizationalUnit;
			Name = simpleOrCompositeName;
		}
		else
		{
			var split = simpleOrCompositeName.Split(Models.Constants.ORGANIZATIONAL_UNIT_AND_KEY_DELIMITER);
			if (split.Length != 2)
			{
				throw new ArgumentException(
					$"Composite name containing an organizational unit cannot have more or less one '{Models.Constants.ORGANIZATIONAL_UNIT_AND_KEY_DELIMITER}' character because it denotes the division between organizational unit and name.");
			}
			OrganizationalUnit = split[0];
			Name = split[1];
		}
	}
	public OrganizationalUnit OrganizationalUnit { get; private set; }
	public string Name { get; set; }

	public override string ToString()
	{
		return $"{OrganizationalUnit}{Models.Constants.ORGANIZATIONAL_UNIT_AND_KEY_DELIMITER}{Name}";
	}

	public static implicit operator String(ConfigurationKey key) => key.ToString() ;
	public static ConfigurationKey GetPhyrosRootLevelKey(string key) => new ConfigurationKey(key, String.Empty);
}
