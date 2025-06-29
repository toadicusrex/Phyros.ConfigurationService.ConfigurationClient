using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Phyros.ConfigurationService.ConfigurationClient.ConfigurationManagement;
using Phyros.ConfigurationService.ConfigurationClient.Models;
using Phyros.OrganizationalUnits;

namespace Phyros.ConfigurationService.ConfigurationClient.ApiClient;

public class ConfigurationApiClient : IConfigurationApiReader, IClientStatusProvider, IConfigurationApiValueWriter, IClientInformationProvider
{
	private readonly HttpClient _httpClient;

	private static readonly JsonSerializerOptions _serializerOptions;

	static ConfigurationApiClient()
	{
		_serializerOptions = new JsonSerializerOptions()
		{
			UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
			AllowTrailingCommas = false,
			PropertyNameCaseInsensitive = true,
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			NumberHandling = JsonNumberHandling.WriteAsString
		};
		_serializerOptions.Converters.Add(new AutoNumberToStringConverter());
	}
	public ConfigurationApiClient(HttpClient httpClient, IConfiguration configuration)
	{
		_httpClient = httpClient;
	}

	public async Task<bool> PingAsync()
	{
		var result = await _httpClient.GetAsync("diagnostics/heartbeat");
		return result.IsSuccessStatusCode;
	}

	public async Task<IEnumerable<IConfigurationSetting>?> LoadConfigurationGroupAsync(OrganizationalUnit organizationalUnit, string configurationGroupName)
	{
		var result = await _httpClient.GetAsync($"configuration/{organizationalUnit.ToUrlString()}/group/{configurationGroupName}");
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			var settings = JsonSerializer.Deserialize<IEnumerable<PhyrosConfigurationSetting>>(content, _serializerOptions);
			return settings;
		}

		throw new Exception(result.ReasonPhrase);
	}

	public async Task<IEnumerable<IConfigurationSetting>?> LoadUngroupedSettingsAsync(OrganizationalUnit organizationalUnit, IEnumerable<string> keys)
	{
		var result = await _httpClient.PostAsync($"configurationlist/{organizationalUnit.ToUrlString()}", new StringContent(JsonSerializer.Serialize(keys), Encoding.UTF8, "application/json" ));
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			var settings = JsonSerializer.Deserialize<IEnumerable<PhyrosConfigurationSetting>>(content, _serializerOptions);
			return settings;
		}

		throw new Exception(result.ReasonPhrase);
	}

	public async Task<IConfigurationSetting> AddAdditionalConfigurationSettingAsync(OrganizationalUnit organizationalUnit, string configurationGroupName, string key)
	{
		var result = await _httpClient.PostAsync($"configurationgroup/{organizationalUnit.ToUrlString()}/{configurationGroupName}/add/{key}", null);
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			var deserialized = JsonSerializer.Deserialize<PhyrosConfigurationSetting>(content, _serializerOptions);
			if (String.IsNullOrWhiteSpace(deserialized?.Value))
			{
				return new NullConfigurationSetting() { Key = key, OrganizationalUnit = deserialized?.OrganizationalUnit ?? organizationalUnit, Content = content, Status = result.StatusCode };
			}

			return deserialized;
		}

		return new NullConfigurationSetting() { Key = key, OrganizationalUnit = organizationalUnit, Content = content, Status = result.StatusCode };
	}

	public async Task<IConfigurationSetting> GetConfigurationSettingAsync(OrganizationalUnit organizationalUnit, string key)
	{
		var result = await _httpClient.GetAsync($"configuration/{organizationalUnit.ToUrlString()}/{key}");
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			return JsonSerializer.Deserialize<PhyrosConfigurationSetting>(content, _serializerOptions)!;
		}

		return new NullConfigurationSetting() { Key = key, Content = content, Status = result.StatusCode };
	}

	public async Task<string> GetClientConnectionString(OrganizationalUnit organizationalUnit)
	{
		var result = await _httpClient.GetAsync($"tenancyinformation/connectionstring/{organizationalUnit.ToUrlString()}");
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			return content;
		}
		throw new Exception(result.ReasonPhrase);
	}

	public async Task SetValue(ConfigurationKey configurationKey, PhyrosConfigurationSetting value)
	{
		var body = new ConfigurationSettingChangeDto(value.Value, value.ValueType, value.Locked);
		var result = await _httpClient.PutAsync($"configuration/{configurationKey.OrganizationalUnit.ToUrlString()}/{configurationKey.Name}",  new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));
		var content = await result.Content.ReadAsStringAsync();
		if (!result.IsSuccessStatusCode)
		{
			throw new ConfigurationChangeFailedException(result);
		}
	}

	public async Task<bool> IsActive(OrganizationalUnit organizationalUnit)
	{
		var result = await _httpClient.GetAsync($"tenancyinformation/status/{organizationalUnit.ToUrlString()}");
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			return content == "Active";
		}

		throw new Exception(result.ReasonPhrase);
	}

	public async Task<IEnumerable<string>> GetActiveClientNames()
	{
		var result = await _httpClient.GetAsync($"tenancyinformation/active");
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			return JsonSerializer.Deserialize<IEnumerable<string>>(content, _serializerOptions)!;
		}
		throw new Exception(result.ReasonPhrase);
	}

	public async Task<IEnumerable<ClientInformationDto>> GetActiveClientInformation()
	{
		var result = await _httpClient.GetAsync($"tenancyinformation/info/active");
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			return JsonSerializer.Deserialize<IEnumerable<ClientInformationDto>>(content, _serializerOptions)!;
		}
		throw new Exception(result.ReasonPhrase);
	}

	public async Task<ClientInformationDto?> GetClientInformation(string clientName)
	{
		var result = await _httpClient.GetAsync($"tenancyinformation/info/{clientName}");
		var content = await result.Content.ReadAsStringAsync();
		if (result.IsSuccessStatusCode)
		{
			return JsonSerializer.Deserialize<ClientInformationDto>(content, _serializerOptions)!;
		}
		throw new Exception(result.ReasonPhrase);
	}

	private class AutoNumberToStringConverter : JsonConverter<object>
	{
		public override bool CanConvert(Type typeToConvert)
		{
			return typeof(string) == typeToConvert;
		}
		public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType == JsonTokenType.Number)
			{
				return reader.TryGetInt64(out long l) ?
					l.ToString() :
					reader.GetDouble().ToString();
			}
			if (reader.TokenType == JsonTokenType.String)
			{
				return reader.GetString();
			}
			using JsonDocument document = JsonDocument.ParseValue(ref reader);
			return document.RootElement.Clone().ToString();
		}

		public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
		{
			writer.WriteStringValue(value.ToString());
		}
	}
}
