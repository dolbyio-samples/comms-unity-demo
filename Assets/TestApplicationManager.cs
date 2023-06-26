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


public class TestMySink : VideoSink
{
    public string ParticipantIdLeft { get; set; }
    public string ParticipantIdRight { get; set; }

    public bool IsFocused { get; set; }

    private VideoRenderer _rendererLeft;
    private VideoRenderer _rendererRight;

    public TestMySink(GameObject displayLeft, GameObject displayRight)
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
            else if(splits[2].Equals(ParticipantIdRight))
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

public class TestApplicationManager : MonoBehaviour
{
    private DolbyIOSDK _sdk = DolbyIOManager.Sdk;
    private MySink _sink;

    public GameObject StageDisplayLeft;
    public GameObject StageDisplayRight;

    void Awake()
    { 
        _sink = new MySink(StageDisplayLeft, StageDisplayRight);
    }

    async void Start()
    {

    }

    void OnApplicationFocus(bool focused)
    {
        _sink.IsFocused = focused;
        if (focused)
        {
            DolbyIOManager.ClearQueue();
        }
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
