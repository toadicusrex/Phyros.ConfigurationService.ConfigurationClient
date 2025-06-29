using Phyros.OrganizationalUnits;

namespace Phyros.Configuration.Client.Models;
public class ManagedKey
{
	public static string GetManagedKey(OrganizationalUnit organizationalUnit, string key)
	{
		return organizationalUnit == "" ? key : $"{organizationalUnit}|{key}";
	}

	public OrganizationalUnit DefaultOrganizationalUnit { get; private set; } = "";
	public OrganizationalUnit OrganizationalUnit { get; private set; } = "";
	public string Key { get; private set; } = null!;
	public string Path { get; private set; } = string.Empty;

	public static ManagedKey Parse(string key, string defaultOrganizationalUnit)
	{
		var returnValue = new ManagedKey { DefaultOrganizationalUnit = defaultOrganizationalUnit };

		// remove the connection strings group, we're just using flat settings.
		string targetKey = key;
		if (targetKey.StartsWith(Constants.CONNECTION_STRINGS_CONFIG_GROUP))
		{
			targetKey = targetKey.Substring(Constants.CONNECTION_STRINGS_CONFIG_GROUP.Length + 1);
		}
		if (String.IsNullOrWhiteSpace(key))
		{
			throw new ArgumentException("Parameter cannot be null or whitespace, or contain only 'ConnectionStrings'.", nameof(key));
		}
		targetKey = ExtractOrganizationalUnit(targetKey, defaultOrganizationalUnit, out var organizationalUnit);
		returnValue.OrganizationalUnit = organizationalUnit;

		targetKey = ParsePath(targetKey, out var path);
		returnValue.Key = targetKey;
		returnValue.Path = path;

		return returnValue;
	}

	private static string ParsePath(string key, out string path)
	{
		path = String.Empty;
		var pathDelimiterIndex = key.IndexOf(':');
		if (pathDelimiterIndex > -1)
		{
			path = key.Substring(pathDelimiterIndex + 1);
		}
		return pathDelimiterIndex == -1 ? key : key.Substring(0, pathDelimiterIndex);
	}

	private static string ExtractOrganizationalUnit(string key, string defaultOrganizationalUnit, out OrganizationalUnit organizationalUnit)
	{
		var split = key.Split('|');
		string remainderWithoutOrgUnit;
		if (split.Length == 1)
		{
			organizationalUnit = defaultOrganizationalUnit;
			return key;
		}
		else if (split.Length == 2)
		{
			organizationalUnit = split[0];
			return split[1];
		}

		throw new ArgumentException("Name may not contain more than one pipe - pipes are used to denote organizational units.", key);
	}

	public override string ToString()
	{
		if (OrganizationalUnit == "")
		{
			return Key;
		}
		if (OrganizationalUnit.Equals(DefaultOrganizationalUnit))
		{
			return Key;
		}
		if (DefaultOrganizationalUnit.IsDescendantOf(OrganizationalUnit))
		{
			return Key;
		}
		return $"{OrganizationalUnit}|{Key}";
	}

	public static implicit operator string(ManagedKey key) => key.ToString();

	public static ManagedKey Generate(OrganizationalUnit organizationalUnit, string messageKey, OrganizationalUnit defaultOrganizationalUnit)
	{
		var key = ParsePath(messageKey, out var path);
		return new ManagedKey()
		{
			OrganizationalUnit = organizationalUnit,
			Key = key,
			DefaultOrganizationalUnit = defaultOrganizationalUnit,
			Path = path
		};
	}
}
