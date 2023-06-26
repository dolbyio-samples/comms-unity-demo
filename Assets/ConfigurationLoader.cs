using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using UnityEngine;
using UnityEngine.UIElements;

using DolbyIO.Comms;
using DolbyIO.Comms.Unity;


public class ConfigurationLoader : MonoBehaviour
{

    public PubNubInitializer PubNubInitializer;
 
    internal Configuration Configuration;

    private HttpClient _client = new HttpClient();

    private async Task<string> GetToken()
    {
        return await Task.Run(async () =>
        {
            string result = "";

            if (!String.IsNullOrEmpty(Configuration.TokenServerUrl))
            {
                HttpResponseMessage response = await _client.GetAsync($"{Configuration.TokenServerUrl}");
                string jsonString = await response.Content.ReadAsStringAsync();

                var token = JsonConvert.DeserializeObject<TokenJson>(jsonString);
                result = token.access_token;
            }
            else
            {
                result = Configuration.ClientAccessToken;
            }

            if (String.IsNullOrEmpty(result))
            {
                throw new Exception("Failed to fetch Access Token");
            }

            return result;
        })
        .ConfigureAwait(false);
    }

    void Awake()
    {
        // Loading configuration
        try
        {
            Configuration = Configuration.Load();
            PubNubInitializer.Configuration = Configuration;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load configuration file: {e.Message}");
        }



    }

}