using System.Text.Json;

// ReSharper disable once CheckNamespace
namespace System;

public static class JsonElementExtensions
{
	private static readonly char[] _separator = [':'];

	public static string? QueryJsonPath(this string value, string xpath, JsonSerializerOptions? options = null) => JsonSerializer.Deserialize<JsonElement>(value, options).GetJsonElement(xpath).GetJsonElementValue();


	public static JsonElement GetJsonElement(this JsonElement jsonElement, string xpath)
	{
		if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
		{
			return default;
		}
		var segments = xpath.Split(_separator, StringSplitOptions.RemoveEmptyEntries);
		foreach (var segment in segments)
		{
			if (int.TryParse(segment, out var index) && jsonElement.ValueKind == JsonValueKind.Array)
			{
				jsonElement = jsonElement.EnumerateArray().ElementAtOrDefault(index);
				if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
				{
					return default;
				}
				continue;
			}
			jsonElement = jsonElement.TryGetProperty(segment, out var value) ? value : default;
			if (jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
			{
				return default;
			}
		}
		return jsonElement;
	}

	public static string? GetJsonElementValue(this JsonElement jsonElement)
	{
		return jsonElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
			? default
			: jsonElement.ToString();
	}
}
