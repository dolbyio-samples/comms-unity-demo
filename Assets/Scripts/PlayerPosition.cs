using System;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UIElements;

public class JsonVector3 
{
    public float x, y, z = 0.0f;

    public JsonVector3(Vector3 vec)
    {
        x = vec.x;
        y = vec.y;
        z = vec.z;
    }
}

class PlayerPosition
{
    public string participantId { get; set; }
    public JsonVector3 position { get; set; }

    public PlayerPosition(string id, Vector3 pos)
    {
        participantId = id;
        position = new JsonVector3(pos);
    }
}

class PlayerDirection
{
    public string participantId { get; set; }
    public JsonVector3 direction { get; set; }


    public PlayerDirection(string id, Vector3 dir)
    {
        participantId = id;
        direction = new JsonVector3(dir);
    }
}