using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine;
using TMPro;
using DolbyIO.Comms;
using DolbyIO.Comms.Unity;
using Newtonsoft.Json;
using PubNubAPI;

public class ConferenceSpawner : MonoBehaviour
{
    private DolbyIOSDK _sdk = DolbyIOManager.Sdk;
    private GameObject _participantAvatar;

    private Dictionary<string, GameObject> _participants = new Dictionary<string, GameObject>();
    private Dictionary<string, VideoTrack> _videoTracks = new Dictionary<string, VideoTrack>();

    private List<Action> _backlog = new List<Action>();

    private int _index = 0;

    private string _conferenceId = "";

    public Configuration Configuration { get; set; }

    public static PubNub PubNub = null;

    void Awake()
    {
        _participantAvatar = Resources.Load("ParticipantAvatar") as GameObject;
    }

    void Start()
    {
    }

    public async Task Init(string conferenceId)
    {
        _conferenceId = conferenceId;

        var config  = new PNConfiguration();
        config.SubscribeKey = Configuration.PubNub.SubscribeKey;
        config.PublishKey = Configuration.PubNub.PublishKey;
        config.SecretKey = Configuration.PubNub.SecretKey;

        config.ReconnectionPolicy = PNReconnectionPolicy.LINEAR;

        config.LogVerbosity = PNLogVerbosity.BODY;
        config.UserId = _sdk.Session.User.Id;

        if (PubNub == null)
        {
            PubNub = new PubNub(config);
            PubNub.SubscribeCallback += SubscribeHandler;
        }

        PubNub.Subscribe()
                .Channels(new List<string> { conferenceId })
                .Execute();
        
    }
    
    public void Release()
    {
        PubNub.Unsubscribe()
            .Channels(new List<string> { _conferenceId })
            .Async((result, status) =>
            {
                if (status.Error)
                {
                    Debug.LogError("Failed to unsubscribe to channel");
                }
            });

        foreach (var (k, v) in _participants) 
        {
            Destroy(v);
        }
        _participants.Clear();
        _videoTracks.Clear();
        _backlog.Clear();
    }

    void SubscribeHandler(object sender, EventArgs e)
    {
        SubscribeEventEventArgs mea = e as SubscribeEventEventArgs;
            
        if (mea.MessageResult != null)
        {
            var msg = mea.MessageResult.Payload as Dictionary<string, object>;
            UpdatePositions(msg);
        }
        
    }

    public void OnActiveSpeakerChange(string conferenceId, int count, string[] activeSpeakers)
    {
        if (count > 0)
        {
            lock(_backlog)
            {
                _backlog.Add(() =>
                {
                    try
                    {
                        foreach (var (id, participant) in _participants)
                        {
                            ParticipantController participantController = participant.GetComponentInChildren<ParticipantController>();
                            string? speaker = Array.Find(activeSpeakers, a => a.Equals(participantController.Participant.Id));
                            if (speaker != null)
                            {
                                participantController.Speaking = true;
                            }
                            else
                            {
                                participantController.Speaking = false;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to set active speaker {e.Message}");
                    }

                });
            }
        }
    }

    void UpdatePositions(Dictionary<string, object> message) {
        try
        {
            lock (_backlog)
            {
                var participantId = message["participantId"].ToString();
                object result;

                GameObject participant;
                if (_participants.TryGetValue(participantId, out participant))
                {
                    ParticipantController participantController = participant.GetComponentInChildren<ParticipantController>();

                    if (message.TryGetValue("position", out result))
                    {
                        var position = result as Dictionary<string, object>;
                        _backlog.Add(() =>
                        {
                            participantController.MoveToWorldCoordinates
                            (
                                new Vector3
                                (
                                    float.Parse(position["x"].ToString()),
                                    1.0f,
                                    -float.Parse(position["z"].ToString())
                                )
                            );
                        });
                    }


                    if (message.TryGetValue("direction", out result))
                    {
                        var direction = result as Dictionary<string, object>;
                        _backlog.Add(() =>
                        {
                            participantController.LookAt
                            (
                                new Vector3
                                (
                                    float.Parse(direction["x"].ToString()),
                                    float.Parse(direction["y"].ToString()),
                                    float.Parse(direction["z"].ToString())
                                )
                            );
                        });
                    }
                }
            }
            
        }
        catch (Exception e)
        {
            //Debug.LogError(snapshot);
            Debug.LogError($"Failed to update positions: {e.Message}");
        }
    }

    void Update()
    {
        lock(_backlog)
        {
            if (_backlog.Count > 0)
            {
                foreach(var action in _backlog)
                {
                    action();
                }
                _backlog.Clear();
            }
        }
    }

    private void DecodeMetadata(string data) {

    }

    public GameObject GetGameObjectForParticipant(string id)
    {
        GameObject obj = null;

        lock(_backlog)
        {
            _participants.TryGetValue(id, out obj);
        }

        return obj;
    }

    private void UpdateVideoViews()
    {
        foreach (var kvp in _participants)
        {
            VideoTrack track;
            if (_videoTracks.TryGetValue(kvp.Key, out track))
            {
                lock(_backlog)
                {
                    _backlog.Add(() =>
                    {
                        var controller = kvp.Value.GetComponentInChildren<VideoController>();
                        var nameObject = kvp.Value.GetComponentInChildren<TextMeshProUGUI>();

                        if (nameObject)
                        {
                            nameObject.enabled = false;
                        }

                        controller.Show = true;
                        _sdk.Video.Remote.SetVideoSinkAsync(track, controller.Renderer)
                            .ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    Debug.LogWarning(task.Exception.Message);
                                }
                            }, TaskContinuationOptions.OnlyOnFaulted);
                    });
                }
            }
            else
            {
                lock (_backlog)
                {
                    _backlog.Add(() =>
                    {
                        var controller = kvp.Value.GetComponentInChildren<VideoController>();
                        controller.Show = false;

                        var nameObject = kvp.Value.GetComponentInChildren<TextMeshProUGUI>();

                        if (nameObject)
                        {
                            nameObject.enabled = true;
                        }
                    });
                }
                
            }
        }
    }

    private void AddParticipant(Participant p)
    {
        lock(_backlog)
        {
            _backlog.Add(() => 
            {
                if (!_participants.ContainsKey(p.Id))
                {
                    Metadata? metadata = Helpers.DecodeMetadata(p.Info.ExternalId);
                    var initialPosition = new Vector3(-450, 0, -450);
                    if (metadata != null) 
                    {
                        initialPosition = new Vector3(metadata.position.x, 1.0f, -metadata.position.z);
                    }

                    GameObject participant = Instantiate(_participantAvatar, initialPosition, Quaternion.identity);
                    
                    TextMeshProUGUI nameObject = participant.GetComponentInChildren<TextMeshProUGUI>();
                    
                    if (nameObject)
                    {
                        nameObject.SetText(p.Info.Name);
                    }
                    
                    ParticipantController participantController = participant.GetComponentInChildren<ParticipantController>();
                    participantController.Init(_conferenceId, p);
                    participantController.MoveToWorldCoordinates(new Vector3(initialPosition.x, 0.0f, initialPosition.z));
                    if (metadata != null)
                    {
                        participantController.LookAt(new Vector3(0.0f, metadata.position.r, 0.0f));
                    }
                    _participants.Add(p.Id, participant);
                    _index++;
                }
            });
        }
        UpdateVideoViews();
    }

    private void RemoveParticipant(string userId)
    {
        lock(_backlog)
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

    public void OnParticipantAdded(Participant p)
    {
        if (ParticipantStatus.OnAir == (ParticipantStatus) p.Status && _sdk.Session.User.Id != p.Id)
        {
            AddParticipant(p);
        }
    }

    public void OnParticipantUpdated(Participant p)
    {
        if (ParticipantStatus.OnAir == (ParticipantStatus) p.Status && _sdk.Session.User.Id != p.Id)
        {
            AddParticipant(p);
        } 
        else if (ParticipantStatus.Left == (ParticipantStatus) p.Status && _sdk.Session.User.Id != p.Id)
        {
            RemoveParticipant(p.Id);
        }
    }

    public void OnVideoTrackAdded(VideoTrack track)
    {
        Debug.Log($"VideoTrack added for: {track.ParticipantId}");
        _videoTracks.Add(track.ParticipantId, track);
        UpdateVideoViews();
    }
    
    public void OnVideoTrackRemoved(VideoTrack track)
    {
        _videoTracks.Remove(track.ParticipantId);
        UpdateVideoViews();
    }
}
