using Microsoft.AspNetCore.Mvc;
using Phyros.ConfigurationService.ConfigurationClient.ApiClient;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.Models;

namespace Phyros.ConfigurationService.ConfigurationClient.Tester.Controllers;

public class ConfigurationController : Controller
{
	private readonly IConfiguration _configuration;
	private readonly IConfigurationApiReader _configurationApiClient;
	private readonly IConfigurationApiValueWriter _editor;
	private readonly IClientInformationProvider _clientInformationProvider;

	public ConfigurationController(IConfiguration configuration, IConfigurationApiReader configurationApiClient, 
		IConfigurationApiValueWriter editor, IClientInformationProvider clientInformationProvider)
	{
		_configuration = configuration;
		_configurationApiClient = configurationApiClient;
		_editor = editor;
		_clientInformationProvider = clientInformationProvider;
	}
	[HttpGet]
	[Route("/configuration/{key}")]
	public IActionResult RetrieveSingleValue(string key)
	{
		return Ok(_configuration[key]);
	}

	[HttpGet]
	[Route("/connectionstring/{key}")]
	public IActionResult RetrieveConnectionString(string key)
	{
		return Ok(_configuration.GetConnectionString(key));
	}

	[HttpPost]
	[Route("/configurationlist/{organizationalUnitString}")]
	public async Task<IActionResult> RetrieveMultipleUngroupedConfigurationValues(string organizationalUnitString, string[] keys)
	{
		return Ok(await _configurationApiClient.LoadUngroupedSettingsAsync(organizationalUnitString, keys));
	}

	[HttpPut]
	[Route("/configuration/{key}")]
	public async Task<IActionResult> UpdateConfigurationSetting(string key, [FromBody] ConfigurationSettingChangeDto setting)
	{
		await _editor.SetValue(new ConfigurationKey(key, "tester"), new PhyrosConfigurationSetting()
		{
			Value = setting.Value,
			Key = key,
			Locked = setting.Locked,
			OrganizationalUnit = "tester",
			ValueType =setting.ValueType
		});
		return Ok();
	}

	[HttpPut]
	[Route("/configuration/bool/{key}")]
	public IActionResult SetBooleanConfigurationValue(string key, bool value)
	{
		_configuration[key] = value.ToString();
		return Ok();
	}

	[HttpGet]
	[Route("/tenancyconfiguration/active")]
	public async Task<IActionResult> GetActiveTenantNames()
	{
		return Ok(await _clientInformationProvider.GetActiveClientNames());
	}

	[HttpGet]
	[Route("/tenancyconfiguration/info/active")]
	public async Task<IActionResult> GetActiveTenantInformation()
	{
		return Ok(await _clientInformationProvider.GetActiveClientInformation());
	}
}
