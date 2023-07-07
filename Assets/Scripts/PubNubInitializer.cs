using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PubNubAPI;
using DolbyIO.Comms;
using DolbyIO.Comms.Unity;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;



    public class PubNubInitializer : MonoBehaviour
    {
        private DolbyIOSDK _sdk = DolbyIOManager.Sdk;
        public static PubNub PubNub = null;
        private Dictionary<string, GameObject> _participants = new Dictionary<string, GameObject>();
        private List<Action> _backlog = new List<Action>();
        public Configuration Configuration { get; set; }
        private string _conferenceId = "";

        private string _conferenceAlias;

        public string ConferenceAlias
        {
            get => _conferenceAlias;
            set => _conferenceAlias = value;
        }

        // Start is called before the first frame update
        void Start()
        {
            TestConferenceController controller = GetComponent<TestConferenceController>();
        }

        public void Init(string conferenceId)
        {
            Debug.Log(conferenceId);
            _conferenceId = conferenceId;
            var config = new PNConfiguration();
         config.SubscribeKey = Configuration.PubNub.SubscribeKey;
          config.PublishKey = Configuration.PubNub.PublishKey;
        config.SecretKey = Configuration.PubNub.SecretKey;

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

        void UpdatePositions(Dictionary<string, object> message)
        {
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
                            Debug.Log($"Received position for {participantId}");
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
            // Update is called once per frame
            void Update()
            {

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

