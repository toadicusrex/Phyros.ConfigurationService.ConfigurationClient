using System.Net;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;

public class MultitenantConfigurationDataStore
{
	private readonly Dictionary<string, IConfigurationSetting> _settingsStore = new();
	private readonly SemaphoreSlim _settingsStoreLock = new(1, 1);
	public bool InitialLoadComplete { get; private set; }

	public Dictionary<string, IConfigurationSetting> GetSnapshot()
	{
		try
		{
			_settingsStoreLock.Wait();
			return _settingsStore.ToDictionary(x => x.Key, x => x.Value);
		}
		finally
		{
			_settingsStoreLock.Release();
		}
	}

	public IEnumerable<string> GetKeys()
	{
		try
		{
			_settingsStoreLock.Wait();
			return _settingsStore.Keys.ToList();
		}
		finally
		{
			_settingsStoreLock.Release();
		}
	}

	public IConfigurationSetting GetValue(ConfigurationKey key)
	{
		try
		{
			_settingsStoreLock.Wait();
			return !_settingsStore.TryGetValue(key, out var val) ? new NullConfigurationSetting() { Content = "No content", Key = key.Name, OrganizationalUnit = key.OrganizationalUnit, Status = HttpStatusCode.NotFound } : val;
		}
		finally
		{
			_settingsStoreLock.Release();
		}
	}

	public bool TryGetValue(ConfigurationKey key, out IConfigurationSetting? thisNode)
	{
		try
		{
			_settingsStoreLock.Wait();
			var found = _settingsStore.TryGetValue(key, out var val);
			thisNode = !found ? new NullConfigurationSetting() { Content = "No content", Key = key.Name, OrganizationalUnit = key.OrganizationalUnit, Status = HttpStatusCode.NotFound } : val;
			return found;
		}
		finally
		{
			_settingsStoreLock.Release();
		}
	}

	public IConfigurationSetting SetValue(ConfigurationKey key, IConfigurationSetting value)
	{
		try
		{
			_settingsStoreLock.Wait();
			_settingsStore[key.ToString()] = value;
			return value;
		}
		finally
		{
			_settingsStoreLock.Release();
		}
	}

	public bool ContainsKey(string key)
	{
		try
		{
			_settingsStoreLock.Wait();
			return _settingsStore.ContainsKey(key);
		}
		finally
		{
			_settingsStoreLock.Release();
		}
	}

	/// <summary>
	/// Walks the data tree for values for the key.
	/// </summary>
	/// <param name="key">The key.</param>
	/// <param name="getSettingFunction">The get setting function.</param>
	/// <param name="setConnectionString">The set connection string.</param>
	/// <returns></returns>
	internal IConfigurationSetting SearchConfigurationTree(ConfigurationKey key, Func<OrganizationalUnit, IConfigurationSetting> getSettingFunction, Action<ConfigurationKey, string> setConnectionString)
	{
		var organizationalUnitNodes = key.OrganizationalUnit.GetFullyQualifiedNodes().OrderBy(x => x.Length);
		IConfigurationSetting currentValue = new NullConfigurationSetting() { Content = "No content", Key = key.Name, OrganizationalUnit = key.OrganizationalUnit, Status = HttpStatusCode.NotFound };
		// starting from the bottom, we loop through the nodes.  If we find a locked one, we stop and return it.
		try
		{
			_settingsStoreLock.Wait();
			foreach (var currentOrganizationalUnitNode in organizationalUnitNodes)
			{
				var thisNodeKey = new ConfigurationKey(key.Name, currentOrganizationalUnitNode);
				if (!_settingsStore.TryGetValue(thisNodeKey, out var thisNode))
				{
					thisNode = getSettingFunction(currentOrganizationalUnitNode);
					if (thisNode is PhyrosConfigurationSetting targetSetting)
					{
						if (targetSetting.OrganizationalUnit == currentOrganizationalUnitNode)
						{
							_settingsStore[thisNodeKey] = targetSetting;
						}
						else
						{
							_settingsStore[thisNodeKey] = new NullConfigurationSetting()
							{
								Key = key,
								OrganizationalUnit = currentOrganizationalUnitNode,
								Status = HttpStatusCode.NotFound,
								Content = "Retrieved settings node is inherited from a more general organizational unit's node."
							};
						}
					}
					else
					{
						_settingsStore[thisNodeKey] = thisNode; // this is probably a null key setting.
					}

					if (thisNode is PhyrosConfigurationSetting
						{
							ValueType: Models.Constants.CONNECTION_STRING_VALUE_TYPE
						} node)
					{
						setConnectionString(key, node.Value ?? string.Empty);
					}
				}


				switch (thisNode)
				{
					case PhyrosConfigurationSetting { Locked: true } valuedSetting:
						return valuedSetting;
					case PhyrosConfigurationSetting valuedSetting:
						currentValue = valuedSetting;
						break;
					case NullConfigurationSetting nullSetting:
						{
							// if current value is null, there's no reason to replace a setting from lower in the tree.
							if (currentValue is not PhyrosConfigurationSetting)
							{
								currentValue = nullSetting;
							}

							break;
						}
				}
			}
		}
		finally
		{
			_settingsStoreLock.Release();
		}
		return currentValue;
	}

	internal void MarkAsInitialLoadComplete()
	{
		InitialLoadComplete = true;
	}
}
