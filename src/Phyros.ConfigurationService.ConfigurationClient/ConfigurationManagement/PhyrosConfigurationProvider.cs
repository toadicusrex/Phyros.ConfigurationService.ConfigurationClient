using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.ConfigurationService.ConfigurationClient.Wireup;
using Phyros.OrganizationalUnits;
using Constants = Phyros.ConfigurationService.ConfigurationClient.Models.Constants;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

internal class PhyrosConfigurationProvider : IConfigurationProvider, IConfigurationMetadataProvider
{
	private readonly IConfigurationApiReader _client;
	private readonly IConfigurationApiValueWriter _settingsWriter;
	private readonly MultitenantConfigurationDataStore _multitenantConfigurationDataStore;
	private readonly string _configurationGroupName;
	private readonly OrganizationalUnit _organizationalUnit;
	private static ConfigurationReloadToken _reloadToken = new();

	public PhyrosConfigurationProvider(IConfigurationApiReader client, IConfigurationApiValueWriter settingsWriter, ConfigurationClientData clientData, MultitenantConfigurationDataStore multitenantConfigurationDataStore)
	{
		_client = client;
		_settingsWriter = settingsWriter;
		_multitenantConfigurationDataStore = multitenantConfigurationDataStore;
		_configurationGroupName = clientData.ConfigurationGroupName;
		_organizationalUnit = clientData.OrganizationalUnit;
	}

	/// <summary>
	/// Returns the immediate descendant configuration keys for a given parent path based on this
	/// <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider">IConfigurationProvider</see>s data and the set of keys returned by all the preceding
	/// <see cref="T:Microsoft.Extensions.Configuration.IConfigurationProvider">IConfigurationProvider</see>s.
	/// </summary>
	/// <param name="earlierKeys">The child keys returned by the preceding providers for the same parent path.</param>
	/// <param name="parentPath">The parent path.</param>
	/// <returns>The child keys.</returns>
	public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
	{
		var childKeyList = earlierKeys.ToList();
		if (String.IsNullOrWhiteSpace(parentPath))
		{
			childKeyList.AddRange(_multitenantConfigurationDataStore.GetKeys());
		}

		if (TryGet(parentPath!, out var value))
		{
			if (!String.IsNullOrWhiteSpace(value))
			{
				// the only time we should have children is when they're JSON objects.
				var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(value));
				if (JsonDocument.TryParseValue(ref reader, out var document))
				{
					if (document.RootElement.ValueKind == JsonValueKind.Object)
					{
						var properties = GetPropertyNamesWithBreadcrumbs(document.RootElement, parentPath);
						childKeyList.AddRange(properties);
					}
				}
			}
		}

		childKeyList.Sort(ConfigurationKeyComparer.Instance);
		return childKeyList;
	}

	/// <summary>Gets the property names with breadcrumbs pointing to its location in the organizational unit tree.</summary>
	/// <param name="element">The element.</param>
	/// <param name="parentPath">The parent path.</param>
	/// <returns>
	///   <br />
	/// </returns>
	private List<string> GetPropertyNamesWithBreadcrumbs(JsonElement element, string? parentPath)
	{
		var propertyNames = new List<string>();

		if (element.ValueKind == JsonValueKind.Object)
		{
			foreach (JsonProperty property in element.EnumerateObject())
			{
				string currentPath = string.IsNullOrEmpty(parentPath) ? property.Name : $"{parentPath}:{property.Name}";
				propertyNames.Add(currentPath);

				if (property.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
				{
					propertyNames.AddRange(GetPropertyNamesWithBreadcrumbs(property.Value, currentPath));
				}
			}
		}
		else if (element.ValueKind == JsonValueKind.Array)
		{
			int index = 0;
			foreach (JsonElement arrayElement in element.EnumerateArray())
			{
				string currentPath = $"{parentPath}[{index}]";
				propertyNames.AddRange(GetPropertyNamesWithBreadcrumbs(arrayElement, currentPath));
				index++;
			}
		}

		return propertyNames;
	}

	/// <summary>Tries to retrieve a value from the configuration store.</summary>
	/// <param name="wholeKey">The whole key.</param>
	/// <param name="value">The value.</param>
	/// <returns>
	///   <br />
	/// </returns>
	public bool TryGet(string wholeKey, out string? value)
	{
		if (!_multitenantConfigurationDataStore.InitialLoadComplete)
		{
			Load();
		}
		var key = ExtractKeyValueAndJsonPath(wholeKey, out var jsonPath);
		if (key.StartsWith(Constants.CONNECTION_STRINGS_CONFIG_GROUP))
		{
			var connectionStringsJson = (_multitenantConfigurationDataStore.GetValue(new ConfigurationKey(Constants.CONNECTION_STRINGS_CONFIG_GROUP, String.Empty)) as PhyrosConfigurationSetting)?.Value ?? "{}";
			value = !String.IsNullOrEmpty(jsonPath) ? connectionStringsJson.QueryJsonPath(jsonPath, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : String.Empty; // if there's a JsonPath, we're looking for one connection string.
			return true;
		}

		var found = SearchConfigurationTreeForKey(new ConfigurationKey(key, _organizationalUnit));
		if (found is PhyrosConfigurationSetting locatedSetting)
		{
			value = !String.IsNullOrEmpty(jsonPath) ? locatedSetting.Value?.QueryJsonPath(jsonPath, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true }) : locatedSetting.Value;
			return true;
		}

		value = null;
		return false;
	}

	/// <summary>
	/// Walks the configuration tree for values for the key.
	/// </summary>
	/// <param name="key">The org unit and key.</param>
	/// <returns></returns>
	private IConfigurationSetting SearchConfigurationTreeForKey(ConfigurationKey key)
	{
		return _multitenantConfigurationDataStore.SearchConfigurationTree(key,
			(orgUnit) =>
			{
				var result = Task.Run<IConfigurationSetting>(async () => await _client.AddAdditionalConfigurationSettingAsync(orgUnit, _configurationGroupName, key.Name))
						.ConfigureAwait(false)
						.GetAwaiter()
						.GetResult();
				return result;
			},
			(connectionString, name) => SetConnectionString(name, connectionString));
	}

	/// <summary>
	/// Sets a connection string.
	/// </summary>
	/// <param name="connectionStringName">Name of the connection string.</param>
	/// <param name="connectionStringValue">The connection string value.</param>
	/// <returns></returns>
	private void SetConnectionString(string connectionStringName, string connectionStringValue)
	{
		// reset the connection strings element for GetConnectionString(name) functionality
		var configurationKey = new ConfigurationKey(Constants.CONNECTION_STRINGS_CONFIG_GROUP, _organizationalUnit);
		var connectionStringsNode = _multitenantConfigurationDataStore.GetValue(configurationKey) as PhyrosConfigurationSetting ??
																																																				new PhyrosConfigurationSetting()
																																																				{
																																																					Key = Constants.CONNECTION_STRINGS_CONFIG_GROUP,
																																																					Locked = false,
																																																					OrganizationalUnit = _organizationalUnit,
																																																					Value = "{}",
																																																					ValueType = "JSON"
																																																				};
		var connectionStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(connectionStringsNode.Value!,
			new JsonSerializerOptions() { PropertyNameCaseInsensitive = true })!;
		if (!connectionStrings.ContainsKey(connectionStringName) ||
				connectionStrings[connectionStringName] != connectionStringValue)
		{
			connectionStrings[connectionStringName] = connectionStringValue;
		}

		connectionStringsNode.Value = JsonSerializer.Serialize(connectionStrings);
		_multitenantConfigurationDataStore.SetValue(configurationKey, connectionStringsNode);

	}

	/// <summary>
	/// Extracts the key value and json path from a whole key (without the configuration key).
	/// </summary>
	/// <param name="wholeKey">The whole key.</param>
	/// <param name="jsonPath">The json path.</param>
	/// <returns></returns>
	/// <exception cref="ArgumentException">Parameter cannot be null or whitespace., nameof(wholeKey)</exception>
	private string ExtractKeyValueAndJsonPath(string wholeKey, out string jsonPath)
	{
		if (String.IsNullOrWhiteSpace(wholeKey))
		{
			throw new ArgumentException("Parameter cannot be null or whitespace.", nameof(wholeKey));
		}
		var key = wholeKey;
		if (key.StartsWith(Constants.CONNECTION_STRINGS_CONFIG_GROUP))
		{
			key = key.Substring(Constants.CONNECTION_STRINGS_CONFIG_GROUP.Length + 1);
		}
		jsonPath = String.Empty;
		var delimiterIndex = key.IndexOf(':');
		if (delimiterIndex > -1)
		{
			jsonPath = key.Substring(delimiterIndex + 1);
		}
		return delimiterIndex == -1 ? key : key.Substring(0, delimiterIndex);
	}

	/// <summary>
	/// Loads configuration from the configuration api for the application's assigned default organizational unit.
	/// </summary>
	/// <returns></returns>
	public void Load()
	{
		if (_multitenantConfigurationDataStore.InitialLoadComplete)
		{
			return;
		}
		var configurationSettings = (Task.Run(async () => await _client
			.LoadConfigurationGroupAsync(_organizationalUnit, _configurationGroupName))
			.ConfigureAwait(false)
			.GetAwaiter()
			.GetResult() ?? Array.Empty<PhyrosConfigurationSetting>()).AsEnumerable()
			.Where(x => x is PhyrosConfigurationSetting)
			.Cast<PhyrosConfigurationSetting>()
			.ToList();
		// add all items to the config dictionary, even connection strings

		foreach (var item in configurationSettings)
		{
			_multitenantConfigurationDataStore.SetValue(new ConfigurationKey(item.Key, item.OrganizationalUnit), item);
		}

		// if the config item is of value type "ConnectionString", add it to a connection strings node.
		var connectionStringDictionary = configurationSettings.Where(x =>
				Constants.CONNECTION_STRING_VALUE_TYPE.Equals(x.ValueType, StringComparison.InvariantCultureIgnoreCase))
			.ToDictionary(x => x.Key, x => x.Value);
		_multitenantConfigurationDataStore.SetValue(ConfigurationKey.GetPhyrosRootLevelKey(Constants.CONNECTION_STRINGS_CONFIG_GROUP), new PhyrosConfigurationSetting()
		{
			Value = JsonSerializer.Serialize(connectionStringDictionary),
			OrganizationalUnit = _organizationalUnit,
			Key = Constants.CONNECTION_STRINGS_CONFIG_GROUP,
			Locked = false,
			ValueType = Constants.CONNECTION_STRING_VALUE_TYPE
		});
		_multitenantConfigurationDataStore.MarkAsInitialLoadComplete();
	}

	/// <summary>
	/// Sets the specified key for the default organizational unit.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="value">The value.</param>
	/// <returns></returns>
	public void Set(string key, string? value)
	{
		/* note that because of the way this is evolving, the fully qualified name may not be used, and we may use
		* just the key name.  This is probably a mistake on my part, but we have both single-tenant and multi-tenant applications, and I didn't have the foresight to always
		* use the multitenant name.  Sorry in advance.  It would be a really wise idea to refactor this so that it always stores the multitenant key (i.e. <Organization Unit Name>|<Configuration Name>)
		*/
		var fullyQualifiedKeyName = $"{_organizationalUnit}{Constants.ORGANIZATIONAL_UNIT_AND_KEY_DELIMITER}{key}";

		var configurationSetting = new PhyrosConfigurationSetting()
		{
			Key = key,
			Locked = false,
			ValueType = "string",
			Value = value
		};
		var configurationKey = new ConfigurationKey(key, _organizationalUnit);
		if (!_multitenantConfigurationDataStore.ContainsKey(fullyQualifiedKeyName))
		{
			// we're going to optimistically set the root-level org unit.
			configurationSetting.OrganizationalUnit = String.Empty;
			configurationKey = new ConfigurationKey(key, String.Empty);
		}
		// Send the value to the Configuration API; if this fails, it should throw an exception and not proceed to set the value on the IConfiguration object.
		_settingsWriter.SetValue(configurationKey, configurationSetting);
		// update the Phyros Configuration Provider data store (and the IConfiguration data, essentially).
		_multitenantConfigurationDataStore.SetValue(configurationKey, configurationSetting);

		OnReload();
	}

	/// <summary>
	/// Called when configuration is reloaded.
	/// </summary>
	/// <returns></returns>
	protected void OnReload()
	{
		ConfigurationReloadToken previousToken = Interlocked.Exchange(ref _reloadToken, new ConfigurationReloadToken());
		previousToken.OnReload();
	}

	/// <summary>
	/// Returns a <see cref="IChangeToken"/> that can be used to listen when this provider is reloaded.
	/// </summary>
	/// <returns>The <see cref="IChangeToken"/>.</returns>
	public IChangeToken GetReloadToken()
	{
		return _reloadToken;
	}

	public bool HasManagedKey(string key)
	{
		var found = SearchConfigurationTreeForKey(new ConfigurationKey(key, _organizationalUnit)) as PhyrosConfigurationSetting;
		return found != null;
	}

	internal Dictionary<string, IConfigurationSetting> GetDataSnapshot()
	{
		return _multitenantConfigurationDataStore.GetSnapshot();
	}
}
