using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DolbyIO.Comms;
using DolbyIO.Comms.Unity;
using Unity.VisualScripting;
using System.Diagnostics;
using System.Numerics;
//using PubNubAPI;

namespace DolbyIO.Comms.Unity
{
    [AddComponentMenu("Dolby.io Comms/Conference Controller", 2)]
    [Inspectable]
    public class TestConferenceController : MonoBehaviour
    {
        private DolbyIOSDK _sdk = DolbyIOManager.Sdk;

        private GameObject _participantAvatar;

        private Dictionary<string, GameObject> _participants = new Dictionary<string, GameObject>();

        private string _conferenceId = "";

        private int _index = 0;

        private List<Action> _backlog = new List<Action>();

        private List<VideoTrack> _tracks = new List<VideoTrack>();

        private List<TestVideoController> _videoControllers = new List<TestVideoController>();

        private VideoFrameHandler _cameraVideoFrameHandler = null;
        private VideoFrameHandler _screenShareVideoFrameHandler = null;

        [Tooltip("The conference alias to join.")]
        [SerializeField]
        private string _conferenceAlias;

        public PubNubInitializer PubNubInitializer;
        //public event Action<Task> onInformationReady;

        [SerializeField]
        private Button joinButton;

        public string ConferenceAlias
        {
            get => _conferenceAlias;
            set => _conferenceAlias = value;
        }

        [Tooltip("The spatial audio style.")]
        public SpatialAudioStyle AudioStyle = SpatialAudioStyle.Shared;

        [Tooltip("The scale of the 3D Environment.")]
        public UnityEngine.Vector3 Scale = new UnityEngine.Vector3(1.0f, 1.0f, 1.0f);

        [Tooltip("Indicates if the conference should be joined automatically.")]
        public bool AutoJoin = false;

        public GameObject VideoDevice;
        public GameObject ScreenShareSource;

        void Awake()
        {
            _participantAvatar = Resources.Load("ParticipantAvatar") as GameObject;
        }

        void Start()
        {
            if (_sdk.IsInitialized)
            {
                _sdk.Conference.VideoTrackAdded = HandleVideoTrackAdded;
                _sdk.Conference.VideoTrackRemoved = HandleVideoTrackRemoved;
                _sdk.Conference.ParticipantUpdated = HandleParticipantUpdated;
            }

            if (AutoJoin)
            {
                Join();
            }

            joinButton.onClick.AddListener(() =>
            {
                var conference = Join();
                _conferenceId = conference.Id;
                PubNubInitializer.Init(conference.Id);

            });
        }

        public Conference Join()
        {
            try
            {
                ConferenceOptions options = new ConferenceOptions();
                options.Alias = _conferenceAlias;
                options.Params.SpatialAudioStyle = AudioStyle;

                JoinOptions joinOpts = new JoinOptions();
                joinOpts.Connection.SpatialAudio = true;
                joinOpts.Constraints.Audio = true;

                Conference conference = _sdk.Conference.CreateAsync(options).Result;
                Conference joinedConference = _sdk.Conference.JoinAsync(conference, joinOpts).Result;

               // string information = conference.Id;

                //PubNubInitializer.Init(information);

                //onInformationReady?.Invoke(information);
    

                // set conference id here

                _sdk.Conference.SetSpatialEnvironmentAsync
                (
                    new System.Numerics.Vector3(Scale.x, Scale.y, Scale.z),  // Scale
                    new System.Numerics.Vector3(0.0f, 0.0f, 1.0f), // Forward
                    new System.Numerics.Vector3(0.0f, 1.0f, 0.0f),  // Up
                    new System.Numerics.Vector3(1.0f, 0.0f, 0.0f)   // Right
                ).Wait();
                return joinedConference;
            }
            catch (DolbyIOException e)
            {
                UnityEngine.Debug.LogError(e.Message);
                return null;
            }

        }

        public void Leave()
        {
            try
            {
                _sdk.Conference.LeaveAsync().Wait();
            }
            catch (DolbyIOException e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }
        }

        public void Mute(bool muted)
        {
            try
            {
                _sdk.Audio.Local.MuteAsync(muted).Wait();
            }
            catch (DolbyIOException e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }
        }

        public void MuteRemote(bool muted, string participantId)
        {
            try
            {
                _sdk.Audio.Remote.MuteAsync(muted, participantId).Wait();
            }
            catch (DolbyIOException e)
            {
                UnityEngine.Debug.LogError(e.Message);
            }
        }

        public void StartVideo()
        {
            VideoDevice? device = null;

            if (VideoDevice)
            {
                var dropdown = VideoDevice.GetComponent<VideoDeviceDropdown>();
                if (dropdown)
                {
                    device = dropdown.CurrentDevice;
                }
            }

            var controller = _videoControllers.Find(c => c.IsLocal == true);
            if (controller)
            {
                _cameraVideoFrameHandler = new VideoFrameHandler();
                _cameraVideoFrameHandler.Sink = controller.VideoRenderer;
            }

            DolbyIOManager.QueueOnMainThread(() =>
            {

                _sdk.Video.Local.StartAsync(device, _cameraVideoFrameHandler).ContinueWith
                (
                    t => UnityEngine.Debug.LogError(t.Exception),
                    TaskContinuationOptions.OnlyOnFaulted
                );

            });
        }

        public void StopVideo()
        {
            try
            {
                _sdk.Video.Local.StopAsync().Wait();
            }
            catch (DolbyIOException e)
            {
                UnityEngine.Debug.LogError("Failed to stop video." + e);
            }
        }

        public void StartScreenShare()
        {
            try
            {
                if (ScreenShareSource)
                {
                    var dropdown = ScreenShareSource.GetComponent<ScreenShareSourceDropdown>();
                    if (dropdown.CurrentSource.Id != 0)
                    {
                        var controller = _videoControllers.Find(c => c.IsLocal && c.IsScreenShare);
                        if (controller)
                        {
                            _screenShareVideoFrameHandler = new VideoFrameHandler();
                            _screenShareVideoFrameHandler.Sink = controller.VideoRenderer;
                        }

                        _sdk.Video.Local.StartScreenShareAsync(dropdown.CurrentSource, _screenShareVideoFrameHandler)
                            .ContinueWith
                            (
                                t => UnityEngine.Debug.LogError(t.Exception),
                                TaskContinuationOptions.OnlyOnFaulted
                            );
                    }
                    else
                    {
                        throw new Exception("No source selected");
                    }
                }
            }
            catch (DolbyIOException e)
            {
                UnityEngine.Debug.LogError("Failed to start screen share" + e);
            }
        }

        public void StopScreenShare()
        {
            try
            {
            }
            catch (DolbyIOException e)
            {
                UnityEngine.Debug.LogError("Failed to stop screen share" + e);
            }
        }

        private void AddParticipant(Participant p)
        {
            lock (_backlog)
            {
                _backlog.Add(() =>
                {
                    if (!_participants.ContainsKey(p.Id))
                    {
                        Metadata? metadata = Helpers.DecodeMetadata(p.Info.ExternalId);
                        var initialPosition = new UnityEngine.Vector3(-6.34f, 9.64f, -2.2f);
                        if (metadata != null)
                        {
                            initialPosition = new UnityEngine.Vector3(metadata.position.x, 11.0f, metadata.position.z);
                        }

                        GameObject participant = Instantiate(_participantAvatar, initialPosition, UnityEngine.Quaternion.identity);

                        TextMeshProUGUI nameObject = participant.GetComponentInChildren<TextMeshProUGUI>();

                        if (nameObject)
                        {
                            nameObject.SetText(p.Info.Name);
                        }

                        ParticipantController participantController = participant.GetComponentInChildren<ParticipantController>();
                        participantController.Init(_conferenceId, p);
                        participantController.MoveToWorldCoordinates(new UnityEngine.Vector3(initialPosition.x, initialPosition.y, initialPosition.z));
                        if (metadata != null)
                        {
                            participantController.LookAt(new UnityEngine.Vector3(0.0f, metadata.position.r, 0.0f));
                        }
                        _participants.Add(p.Id, participant);
                        _index++;
                    }
                });
            }
        }

        private void RemoveParticipant(string userId)
        {
            lock (_backlog)
            {
                _backlog.Add(() => {
                    GameObject participant;
                    if (_participants.TryGetValue(userId, out participant))
                    {
                        Destroy(participant);
                        _participants.Remove(userId);
                    }
                });
            }
        }

        /// For performance reasons, instead of propagating to video controllers the various video track events,
        /// Controllers will register themself to the Conference Controller during the Awake phase.
        internal void RegisterVideoController(TestVideoController controller)
        {
            _videoControllers.Add(controller);
        }

        private void HandleVideoTrackAdded(VideoTrack track)
        {
            _tracks.Add(track);
            UpdateVideoControllers().ContinueWith(t =>
            {
                UnityEngine.Debug.LogWarning(t.Exception.Message);
            },
            TaskContinuationOptions.OnlyOnFaulted);
        }

        private void HandleVideoTrackRemoved(VideoTrack track)
        {
            _tracks.Remove(track);
            UpdateVideoControllers().ContinueWith(t =>
            {
                UnityEngine.Debug.LogWarning(t.Exception.Message);
            },
            TaskContinuationOptions.OnlyOnFaulted);
        }

        private void HandleParticipantUpdated(Participant p)
        {
            if (ParticipantStatus.OnAir == p.Status)
            {
                AddParticipant(p);
                UpdateVideoControllers().ContinueWith(t =>
                {
                    UnityEngine.Debug.LogWarning(t.Exception.Message);
                },
                TaskContinuationOptions.OnlyOnFaulted);
            }
            else if (ParticipantStatus.Left == (p.Status))
            {
                RemoveParticipant(p.Id);
            }
        }

        private async Task UpdateVideoControllers()
        {
            if (!_sdk.Conference.IsInConference)
            {
                return;
            }

            var participants = await _sdk.Conference.GetParticipantsAsync();

            foreach (var c in _videoControllers)
            {
                string participantId = "";

                if (c.IsLocal)
                {
                    return;
                }

                switch (c.FilterBy)
                {
                    case ParticipantFilter.ParticipantId:
                        participantId = c.Filter;
                        break;
                    case ParticipantFilter.Name:
                        Participant p = participants.Find(p => p.Info.Name.Equals(c.Filter));
                        if (p != null)
                        {
                            participantId = p.Id;
                        }
                        break;
                    case ParticipantFilter.ExternalId:
                        Participant p2 = participants.Find(p => p.Info.ExternalId.Equals(c.Filter));
                        if (p2 != null)
                        {
                            participantId = p2.Id;
                        }
                        break;
                }

                if (!String.IsNullOrEmpty(participantId))
                {
                    VideoTrack track = _tracks.Find(t => t.ParticipantId.Equals(participantId));
                    c.UpdateTrack(track);
                }
            }

        }

        void Update()
        {
            lock (_backlog)
            {
                if (_backlog.Count > 0)
                {
                    foreach (var action in _backlog)
                    {
                        action();
                    }
                    _backlog.Clear();
                }
            }
        }
    }

}

