using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

#nullable enable

public class JsonVector4
{
    public float x, y, z, r = 0.0f;

    public JsonVector4(System.Numerics.Vector4 vec)
    {
        x = vec.X;
        y = vec.Y;
        z = vec.Z;
        r = vec.W;
    }
}

public class Metadata
{
    [JsonProperty("init-pos", Required = Required.Always)]
    public JsonVector4 position { get; set; }
}

public class Helpers
{
    public static Metadata? DecodeMetadata(string data)
    {
        Metadata? result = null;

        if (!String.IsNullOrEmpty(data))
        {
            try
            {
                byte[] decodedBytes = Convert.FromBase64String(data);
                string decodedText = Encoding.UTF8.GetString(decodedBytes);

                result = JsonConvert.DeserializeObject<Metadata>(decodedText);

                Debug.Log(decodedText);
            }
            catch (Exception e)
            {
                Debug.Log($"Wrong metadata format {e.Message}");
            }
        }

        return result;
    }
}