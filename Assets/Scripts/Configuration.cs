using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

public class PubNubConfig
{
	[JsonProperty("publish_key")]
	public string PublishKey { get; set; }

	[JsonProperty("subscribe_key")]
    public string SubscribeKey { get; set; }

	[JsonProperty("secret_key")]
    public string SecretKey { get; set; }
}

/// <summary>
/// Simple configuration loader
/// </summary>
public class Configuration
{
    [JsonProperty("token_server_url")]
    public string TokenServerUrl { get; set; }

    [JsonProperty("client_access_token")]
    public string ClientAccessToken { get; set; }

    [JsonProperty("pubnub")]
    internal PubNubConfig PubNub { get; set; }

    [JsonIgnore]
    private static string _configurationPath = Application.dataPath;

    [JsonIgnore]
    private const string _fileName = "config.json";

	public static Configuration Load()
	{
		var file = $"{_configurationPath}/{_fileName}";

        if (File.Exists(file))
		{
            string content = File.ReadAllText($"{_configurationPath}/{_fileName}");
			return JsonConvert.DeserializeObject<Configuration>(content);
        } else
		{
			throw new Exception($"Configuration file {_configurationPath}/{_fileName} does not exists.");
		}
	}
}

