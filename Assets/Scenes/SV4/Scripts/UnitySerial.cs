using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class UnitySerial : MonoBehaviour
{
    /// <summary>
    /// This is an example of how NOT to do serial communication in Unity.
    /// This example uses a blocking call to serialPort.ReadLine() which will dramatically
    /// affect Unity's main thread (frame rate) and potentially freeze the application
    /// </summary>

    SerialPort serialPort;

    void Start()
    {
        serialPort = new SerialPort();
        serialPort.BaudRate = 9600;
        serialPort.PortName = "COM6";
        //serialPort.ReadTimeout = 5;

        try
        {
            serialPort.Open();
            print("portOpen");
        }
        catch (System.Exception ex)
        {
            print(ex.Message);
        }
    }

    void Update()
    {
        try
        {
            string msg = serialPort.ReadLine();
            print(msg);
        }
        catch (System.Exception ex)
        {

        }
    }

    private void OnApplicationQuit()
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
        }
    }
}
