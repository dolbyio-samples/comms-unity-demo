using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;

using UnityEngine;

using DolbyIO.Comms;
using DolbyIO.Comms.Unity;

public class UpdatedLocalPlayerPosition : MonoBehaviour
{
    private DolbyIOSDK _sdk = DolbyIOManager.Sdk;
    private string _conferenceId = "";
    private JsonSerializerSettings _serializerSettings;

    private float _timePosition = 0.0f;
    private float _timeDirection = 0.0f;

    private Vector3 _position;
    private Vector3 _direction;

    void Start()
    {
        _serializerSettings = new JsonSerializerSettings();
        _serializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

        _position = transform.position;
        _direction = transform.rotation.eulerAngles;
    }

    private async Task UpdatePosition()
    {
         _timePosition += Time.deltaTime;

        if (_timePosition >= 0.3)
        {
            _timePosition = 0;

            try
            {
                var userId = _sdk.Session.User.Id;
                var position = new Vector3(transform.position.x, 1.0f, -transform.position.z);

                if ((position - _position).magnitude > 0.1f)
                {
                    _position = position;
                    var msg = new PlayerPosition(userId, position);

                    //var pubnub = ConferenceSpawner.PubNub;
                    var pubnub = TestConferenceController.PubNub;

                    pubnub.Publish()
                        .Channel(_conferenceId)
                        .Message(msg)
                        .Meta(new Dictionary<string, string> { { "type", "UpdatePosition" } })
                        .Async((result, status) =>
                        {
                            if (status.Error)
                            {
                                Debug.LogError(status.Error);
                                Debug.LogError(status.ErrorData.Info);
                            }
                        });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set position: {e.Message}");
            }
        }
    }

    private async Task UpdateDirection()
    {
        _timeDirection += Time.deltaTime;

        if (_timeDirection >= 0.3)
        {
            _timeDirection = 0;

            try
            {
                var userId = _sdk.Session.User.Id;
                var direction = new Vector3(0.0f, transform.rotation.eulerAngles.y, 0.0f);

                if ((direction - _direction).magnitude > 0.1f)
                {
                    _direction = direction;
                    var msg = new PlayerDirection(userId, direction);

                    var pubnub = TestConferenceController.PubNub;
                    pubnub.Publish()
                        .Channel(_conferenceId)
                        .Message(msg)
                        .Meta(new Dictionary<string, string> { { "type", "UpdateDirection" } })
                        .Async((result, status) =>
                        {
                            if (status.Error)
                            {
                                Debug.LogError(status.Error);
                                Debug.LogError(status.ErrorData.Info);
                            }
                        });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to set direction: {e.Message}");
            }
        }
    }

    // Update is called once per frame
    async void Update()
    {
        if(_sdk.IsInitialized && _sdk.Conference.IsInConference)
        {
            if (String.IsNullOrEmpty(_conferenceId))
            {
                var conference = await _sdk.Conference.GetCurrentAsync();
                _conferenceId = conference.Id;
            }

            await UpdatePosition();
            await UpdateDirection();
        }
    }
}
