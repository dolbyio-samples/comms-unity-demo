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

public class TokenJson
{
    public string access_token { get; set; }
}

public class MySink : VideoSink
{
    public string ParticipantIdLeft { get; set; }
    public string ParticipantIdRight { get; set; }

    public bool IsFocused { get; set; }

    private VideoRenderer _rendererLeft;
    private VideoRenderer _rendererRight;

    public MySink(GameObject displayLeft, GameObject displayRight)
    {
        _rendererLeft = new VideoRenderer(displayLeft);
        _rendererRight = new VideoRenderer(displayRight);
        ParticipantIdLeft = "";
        ParticipantIdRight = "";
        IsFocused = true;
    }

    public void ClearParticipant(string id)
    {
        if (ParticipantIdLeft.Equals(id))
        {
            ParticipantIdLeft = "";
            _rendererLeft.Clear();
        }

        if (ParticipantIdRight.Equals(id))
        {
            ParticipantIdRight = "";
            _rendererRight.Clear();
        }
    }

    public void OnFrame(string streamId, string trackId, VideoFrame frame)
    {
        //Debug.Log($"OnFrame: {streamId}");

        var splits = streamId.Split("_", 3);
        if (splits.Length == 3 && IsFocused)
        {
            if (splits[2].Equals(ParticipantIdLeft))
            {
                _rendererLeft.Render(frame);
            }
            else if (splits[2].Equals(ParticipantIdRight))
            {
                _rendererRight.Render(frame);
            }
            else
            {
                frame.Dispose();
            }
        }
        else
        {
            frame.Dispose();
        }
    }

    public override void OnFrame(VideoFrame frame)
    {


    }

}

public class ApplicationManager : MonoBehaviour
{
    private DolbyIOSDK _sdk = DolbyIOManager.Sdk;
    private MySink _sink;

    public GameObject StageDisplayLeft;
    public GameObject StageDisplayRight;

    public ConferenceSpawner ConferenceSpawner;
    public UserInterface UserInterface;

    private HttpClient _client = new HttpClient();

    internal Configuration Configuration;

    private async Task<string> GetToken()
    {
        return await Task.Run(async () => {
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
            ConferenceSpawner.Configuration = Configuration;
        }
        catch(Exception e)
        {
            Debug.LogError($"Failed to load configuration file: {e.Message}");
        }
        

        _sink = new MySink(StageDisplayLeft, StageDisplayRight);
    }

    async void Start()
    {
        try
        {
            string token = await GetToken();

            await _sdk.SetLogLevelAsync(DolbyIO.Comms.LogLevel.Debug);

            await _sdk.InitAsync(token, RefreshToken);

          //  await _sdk.Video.Remote.SetVideoSinkAsync(_sink);
                       
            Debug.Log("DolbyIOSDK Initialized");

            _sdk.Conference.StatusUpdated = OnConferenceStatusUpdated;
            _sdk.Conference.ParticipantAdded = OnParticipantAdded;
            _sdk.Conference.ParticipantUpdated = OnParticipantUpdated;

            _sdk.MediaDevice.AudioDeviceAdded = OnAudioDeviceAdded;
           //_sdk.MediaDevice.AudioDeviceChanged = OnAudioDeviceChanged;
         //   _sdk.MediaDevice.AudioDeviceRemoved = OnAudioDeviceRemoved;

            _sdk.Conference.ActiveSpeakerChange = OnActiveSpeakerChange;

            _sdk.InvalidTokenError = OnInvalidToken;

            await UserInterface.FillAudioDevices();
        }
        catch(Exception e)
        {
            Debug.LogError($"Failed to initialize SDK: {e.Message}");
        }
    }

    void OnApplicationFocus(bool focused)
    {
        _sink.IsFocused = focused;
        if (focused)
        {
            DolbyIOManager.ClearQueue();
        }
    }

    public string RefreshToken()
    {
        return GetToken().Result;
    }

    public void OnActiveSpeakerChange(string conferenceId, int count, string[] activeSpeakers)
    {
        ConferenceSpawner.OnActiveSpeakerChange(conferenceId, count, activeSpeakers);
    }

    public void OnInvalidToken(string reason, string error)
    {
        Debug.LogError($"{reason} - {error}");
    }

    public void OnConferenceStatusUpdated(ConferenceStatus status, String conferenceId) 
    {
        Debug.Log("OnConferenceStatusUpdated: " + status);

        if (ConferenceStatus.Left == status)
        {
            DolbyIOManager.QueueOnMainThread(() =>
            {
                _sink.ClearParticipant(_sink.ParticipantIdLeft);
                _sink.ClearParticipant(_sink.ParticipantIdRight);
            });
        }
    }

    public void OnParticipantAdded(Participant p)
    {

        Debug.Log("OnParticipantAdded: " + p.Info.Name + " " + p.Id + " " + _sdk.Session.User.Id);
        ConferenceSpawner.OnParticipantAdded(p);

    }

    public void OnParticipantUpdated(Participant p)
    {
        Debug.Log("OnParticipantUpdated: " + p.Info.Name + " " + p.Id + " " + _sdk.Session.User.Id);
        if (ParticipantStatus.Left == (ParticipantStatus) p.Status && _sdk.Session.User.Id != p.Id)
        {
            DolbyIOManager.QueueOnMainThread(() =>
            {
                _sink.ClearParticipant(p.Id);
            });
        }
        ConferenceSpawner.OnParticipantUpdated(p);
    }

    public async void OnAudioDeviceAdded(AudioDevice device)
    {
        // Debug.Log("AudioAdded");
        //await UserInterface.FillAudioDevices();
    }

    public async void OnAudioDeviceChanged(AudioDevice device, bool noDevice)
    {
        // Debug.Log("AudioChanged");
        await UserInterface.FillAudioDevices();
    }

    public async void OnAudioDeviceRemoved(byte[] id)
    {
        // Debug.Log("AudioRemoved");
        //await UserInterface.FillAudioDevices();
    }

    public void EnterStage(string participantId, string stageName)
    {
        Debug.Log("Enter stage");
        switch (stageName)
        {
            case "StageLeft":
                if (String.IsNullOrEmpty(_sink.ParticipantIdLeft))
                {
                    _sink.ParticipantIdLeft = participantId;
                }
                break;
            case "StageRight":
                if (String.IsNullOrEmpty(_sink.ParticipantIdRight))
                {
                    _sink.ParticipantIdRight = participantId;
                }
                break;
        }
    }

    public void ExitStage(string participantId, string stageName)
    {
        Debug.Log("Exit Stage");
        _sink.ClearParticipant(participantId);
    }
}
