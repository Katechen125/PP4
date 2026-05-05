using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightRead : MonoBehaviour
{
    Light myLight;

    void Start()
    {
        myLight = this.GetComponent<Light>();
        // SerialThread.singleton.Init("COM3", 115200);
    }

}
