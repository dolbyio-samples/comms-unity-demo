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

    public TestConferenceController TestConferenceController;
 
    internal Configuration Configuration;

    public TextAsset jsonFile;

    private HttpClient _client = new HttpClient();

    public async Task<string> GetToken()
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
                Debug.Log("Grabbed TokenServerURL successfully");
            }
            else
            {
                result = Configuration.ClientAccessToken;
                Debug.Log("Grabbed ClientAccessToken successfully");
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
 
    }

    public async Task Init()
    {
        // Loading configuration
        try
        {
            Configuration = Configuration.LoadTextAsset(jsonFile.text);
            TestConferenceController.Configuration = Configuration;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load configuration file: {e.Message}");
        }
    }




}
