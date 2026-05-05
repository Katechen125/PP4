using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BouncingBall : MonoBehaviour
{
    [SerializeField] PlatformController platformController;
    Rigidbody ballBody;

    void Start()
    {
        ballBody = GetComponent<Rigidbody>();

        print(MapRange(50, 0, 200, 0, 1000));
    }

    void Update()

    {
        float newVal = MapRange(transform.position.y, 0.5f, 4.0f, 0, 255);

        for (int i = 0; i < 6; i++)
        {
            if (i % 2 == 0)
                {
                    platformController.byteValues[i] = (byte)newVal;
                }
                else
               {
                   platformController.byteValues[i] = (byte)(255-newVal);
               }
        }
        //if (SimpleInput.GetKeyDown(KeyCode.W)) 
        //{
        //    platformController.byteValues[0] = 255;
        //    platformController.byteValues[1] = 0;
        //    platformController.byteValues[2] = 255;
        //    platformController.byteValues[3] = 0;
        //    platformController.byteValues[4] = 255;
        //    platformController.byteValues[5] = 0;
        //}
        //else if (SimpleInput.GetKeyDown(KeyCode.S))
        //{
        //   for (int i = 0; i < 6; i++)
        //    {
        //        if (i % 2 == 0)
        //        {
        //            platformController.byteValues[i] = 0;
        //        }
        //        else
        //        {
        //            platformController.byteValues[i] = 255;
        //        }

        //    }
        //}

        // Use the MapRange function to re-map the y position of the ball (transform.position.y)
        // from it's current min/max values into the min/max values of our servos.

        // Use platformController.byteValues[i] to access each servo and set them to a value
        // between 0 and 255. If needed, cast the values to a (byte).

        // You may need to invert the value for the odd numbered servos.
    }

    public static float MapRange(float val, float oldMin, float oldMax, float newMin, float newMax)
    {
        float slope = (newMax - newMin) / (oldMax - oldMin);
        float newVal = newMin + slope * (val - oldMin);
        return Mathf.Clamp(newVal, Mathf.Min(newMin, newMax), Mathf.Max(newMin, newMax));
    }

    private void OnMouseUp()
    {
        // add an up force to the ball when clicking on it
        ballBody.AddForce(Vector3.up * 5, ForceMode.VelocityChange);
    }
}
