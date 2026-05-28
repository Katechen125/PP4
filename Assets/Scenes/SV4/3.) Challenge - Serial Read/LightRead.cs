using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRead : MonoBehaviour, ISerialReader
{
    Light myLight;

    public void OnMessageReceived(string msg)
    {
        string[] splitmsg = msg.Split(',');
        if (splitmsg.Length == 2)
        {
            int light_val = int.Parse(splitmsg[0]);
            myLight.intensity = (light_val/4095.0f) * 20;
        }
    }

    void Start()
    {
        myLight = this.GetComponent<Light>();
        SerialThread.singleton.Init("COM5", 115200, this);

    }

}
