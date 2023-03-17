using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

using DolbyIO.Comms;
using DolbyIO.Comms.Unity;

public class UserInterface : MonoBehaviour
{
    private DolbyIOSDK _sdk = DolbyIOManager.Sdk;

    public ConferenceSpawner ConferenceSpawner;

    private VisualElement _root;
    private Button _joinButton;
    private Button _leaveButton;
    private TextField _avatarName;
    private TextField _conferenceName;
    private DropdownField _audioInputField;

    private Button _startVideoButton;
    private Button _stopVideoButton;

    private Label _version;

    private VideoFrameHandler _videoFrameHandler = new VideoFrameHandler();

    public void Show(bool show)
    {
        if (show)
        {
            _root.style.display = DisplayStyle.Flex;
        }
        else
        {
            _root.style.display = DisplayStyle.None;
        }
    }

    public bool IsVisible()
    {
        return _root.style.display == DisplayStyle.Flex;
    }

    private async Task<Conference> Join(string name, string alias)
    {
        UserInfo user = new UserInfo();
        user.Name = name;
        
        if (!_sdk.Session.IsOpen)
        {
            user = await _sdk.Session.OpenAsync(user);
        }

        ConferenceOptions options = new ConferenceOptions();
        options.Alias = alias;
        options.Params.SpatialAudioStyle = SpatialAudioStyle.Shared;

        JoinOptions joinOpts = new JoinOptions();
        joinOpts.Connection.SpatialAudio = true;
        joinOpts.Constraints.Audio = true;

        Conference conference = await _sdk.Conference.CreateAsync(options);
        Conference joinedConference = await _sdk.Conference.JoinAsync(conference, joinOpts);

        await _sdk.Conference.SetSpatialEnvironmentAsync
        (
            new System.Numerics.Vector3(1.0f, 1.0f, 1.0f),  // Scale
            new System.Numerics.Vector3(0.0f, 0.0f, 1.0f), // Forward
            new System.Numerics.Vector3(0.0f, 1.0f, 0.0f),  // Up
            new System.Numerics.Vector3(1.0f, 0.0f, 0.0f)   // Right
        );

        return joinedConference;
    }

    public async Task FillAudioDevices()
    {
        try
        {
            var devices = await _sdk.MediaDevice.GetAudioDevicesAsync();
            var current = await _sdk.MediaDevice.GetCurrentAudioInputDeviceAsync();

            DolbyIOManager.QueueOnMainThread(() =>
            { 
                var names = devices
                                .FindAll(d => d.Direction == DeviceDirection.Input)
                                .ConvertAll(device => device.Name);

                _audioInputField.choices = names;

                var index = names.FindIndex(d => d.Equals(current.Name));
                _audioInputField.index = index;
            });
        }
        catch (DolbyIOException e)
        {
            Debug.LogError(e.Message);
        }
    }

    void OnEnable()
    {
        Debug.Log("enable");
        _root = GetComponent<UIDocument>().rootVisualElement;
        _joinButton = _root.Q<Button>("JoinButton");
        _leaveButton = _root.Q<Button>("LeaveButton");
        _avatarName = _root.Q<TextField>("AvatarName");
        _conferenceName = _root.Q<TextField>("ConferenceName");
        _audioInputField = _root.Q<DropdownField>("AudioInput");

        _startVideoButton = _root.Q<Button>("StartVideo");
        _stopVideoButton = _root.Q<Button>("StopVideo");

        _version = _root.Q<Label>("Version");
        
        _root.style.display = DisplayStyle.Flex;
        _version.text = Application.version;

        _joinButton.clicked += async () =>
        {
            var conference = await Join(_avatarName.text, _conferenceName.text);
            await ConferenceSpawner.Init(conference.Id);

            Show(false);
        };

        _leaveButton.clicked += async () =>
        {
            await _sdk.Conference.LeaveAsync();
            ConferenceSpawner.Release();
            
            Show(false);
        };

        _startVideoButton.clicked += async () =>
        {
            _videoFrameHandler.Sink = new CameraFeedSink();

            await _sdk.Video.Local.StartAsync(null, null);
        };

        _stopVideoButton.clicked += async () =>
        {
            await _sdk.Video.Local.StopAsync();
        };

        _audioInputField.RegisterValueChangedCallback(async v =>
        {
            var devices = await _sdk.MediaDevice.GetAudioDevicesAsync();
            AudioDevice? device = devices.Find(d => d.Name.Equals(_audioInputField.text));
            Debug.Log(_audioInputField.text);

            if (device.HasValue)
            {
                await _sdk.MediaDevice.SetPreferredAudioInputDeviceAsync(device.Value);
            }
        });

        //await FillAudioDevices();
    }

    class CameraFeedSink : VideoSink 
    {
        public override void OnFrame(string streamId, string trackId, VideoFrame frame)
        {

            Debug.Log($"Received frame for camea: {streamId}");
            frame.Dispose();
        }
    }
}
