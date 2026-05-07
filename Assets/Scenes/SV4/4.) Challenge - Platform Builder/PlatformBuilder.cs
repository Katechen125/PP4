using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlatformBuilder : MonoBehaviour
{
    float hornLength = 12.93f; // *** Replace the 0 with the length of your horn here
    float rodLength = 87.23f; // *** Replace the 0 with the length of your rods here

    public bool showRodLines = true;
    public float[] inputValues = { 0, 0, 0, 0, 0, 0 }; //Sway Surge Heave Pitch Roll Yaw

    Vector3[] platformPointOrigins; // defined later
    Vector3[] platformPoints; // defined later
    Vector3[] basePoints; // defined later
    float platformDefaultHeight = 0;

    float actuatorLength; // used to represent the linear distance our servo/actuator can move (in mm)
    LineRenderGroup platformGroup;
    LineRenderGroup baseGroup;
    public Material lineMaterial;

    void Start()
    {
        actuatorLength = hornLength; // This value represents how much each actuator can move

        // 1.) *** fill in all 6 values with your values from solidworks
        basePoints = new Vector3[6];
        basePoints[0] = new Vector3(-22.82f, -59.67f, 0.0f);
        basePoints[1] = new Vector3(22.82f, -59.67f, 0.0f);
        basePoints[2] = new Vector3(63.09f, 10.07f, 0.0f);
        basePoints[3] = new Vector3(40.27f, 49.60f, 0.0f);
        basePoints[4] = new Vector3(-40.27f, 49.60f, 0.0f);
        basePoints[5] = new Vector3(-63.09f, 10.07f, 0.0f);

        // 2.) *** fill in all 6 values with your values from solidworks
        platformPoints = new Vector3[6];
        platformPoints[0] = new Vector3(-41.88f, -30.59f, 0.0f);
        platformPoints[1] = new Vector3(41.88f, -30.59f, 0.0f);
        platformPoints[2] = new Vector3(47.43f, -20.97f, 0.0f);
        platformPoints[3] = new Vector3(5.55f, 51.57f, 0.0f);
        platformPoints[4] = new Vector3(-5.55f, 51.57f, 0.0f);
        platformPoints[5] = new Vector3(-47.43f, -20.97f, 0.0f);

        // Utility Functions and Setup

        // LineRenderGroup is a (custom class) collection of line renderers, vector references, and parameters.
        baseGroup = new LineRenderGroup(ref basePoints, Color.green, lineMaterial, "BaseGroup", true, true, 1, this.transform);
        platformGroup = new LineRenderGroup(ref platformPoints, Color.cyan, lineMaterial, "PlatformGroup", true, true, 1, this.transform);

        platformPointOrigins = new Vector3[6]; // used to store default/identity values
        System.Array.Copy(platformPoints, platformPointOrigins, platformPoints.Length); // copy the values
        UtilityLib.ConvertV3ToCppSyntax(basePoints, "basePoints"); // Logs values for later use when we copy to c++
        UtilityLib.ConvertV3ToCppSyntax(platformPoints, "platformPoints"); // Logs values for later use when we copy to c++

        CalculateDefaultHeight();
    }

    void CalculateDefaultHeight()
    {
        Vector3 hornDirection = new Vector3(1, 0, 0); // back-left servo faces right when flat
        Vector3 hornEndPoint = basePoints[3] + (hornDirection * hornLength);

        // Calculate default height (Pythagorean theorem)
        // c^2 = a^2 + b^2

        float sideC = rodLength;
        float sideB = (platformPointOrigins[3] - hornEndPoint).magnitude; // Discuss

        // 3.) *** replace the 0 with the equation to calculate sideA
        float sideA = Mathf.Sqrt((sideC * sideC) - (sideB * sideB ));

        platformDefaultHeight = sideA;
       
        // Update the z value (up) with our newly calculated height
        for (int i = 0; i < platformPointOrigins.Length; i++)
        {
            platformPointOrigins[i].z = platformDefaultHeight;
        }
    }

    void Update()
    {
        // Remember our inputValues[] array holds the desired values for:
        // Sway [0], Surge[1], Heave [2], Pitch [3], Roll [4], Yaw [5]

        for (int i = 0; i < platformPoints.Length; i++)
        {
            // Apply the input values to the top platform (rotate, translate, and height)

            // 4.) *** Enter the desired values for pitch, roll, and yaw
            // Rotate the platform based on inputs, remember to convert from degrees to radians
            platformPoints[i] = RotatePoint(i, inputValues[3] * Mathf.Deg2Rad, inputValues[4] * Mathf.Deg2Rad, inputValues[5] * Mathf.Deg2Rad);

            // 5.) *** Translate each point by the input values for sway, surge, and heave
            platformPoints[i] += new Vector3(inputValues[0], inputValues[1], inputValues[2]);

            // 6.) *** Add in the default height
            platformPoints[i].z += platformDefaultHeight; // *** replace zero with our calculated height (platformDefaultHeight);

            // Error check: measure distances and see if the platform is capable
            // of acheiving the desired position/orientation
            if (showRodLines)
            {
                // To determine if the newly positioned platform is in a valid position,
                // we compare the virtual vector length to the actual rod/horn length.

                // Measure the distance (arm/rod length)
                // 7.) *** replace zero with the vector difference magnitude
                float measuredLegnth = (platformPoints[i]-basePoints[i]).magnitude;

                // Check the distance of the requested position compared to the
                // length parameters of our platform (actuator rod or servo/horn)
                float errorDistance = Mathf.Abs(measuredLegnth - rodLength);
                if (errorDistance < actuatorLength)
                {
                    // Valid length
                    Debug.DrawLine(platformPoints[i], basePoints[i], Color.yellow);
                }
                else
                {
                    // Invalid length
                    Debug.DrawLine(platformPoints[i], basePoints[i], Color.red);
                }
            }
        }

        baseGroup.UpdateLineRenderers();
        platformGroup.UpdateLineRenderers();
    }

    // Getters/Setters for inputValues
    public float inputSway
    {
        get { return inputValues[0]; }
        set { inputValues[0] = value; }
    }
    public float inputSurge
    {
        get { return inputValues[1]; }
        set { inputValues[1] = value; }
    }
    public float inputHeave
    {
        get { return inputValues[2]; }
        set { inputValues[2] = value; }
    }
    public float inputPitch
    {
        get { return inputValues[3]; }
        set { inputValues[3] = value; }
    }
    public float inputRoll
    {
        get { return inputValues[4]; }
        set { inputValues[4] = value; }
    }
    public float inputYaw
    {
        get { return inputValues[5]; }
        set { inputValues[5] = value; }
    }

    // Angle and Rotation functions
    Vector3 RotatePoint(int pointIndex, float angRadX, float angRadZ, float angRadY)
    {
        Vector3 point = platformPointOrigins[pointIndex];
        float x = point.x * Mathf.Cos(angRadZ) * Mathf.Cos(angRadY) + point.y * (Mathf.Sin(angRadX) * Mathf.Sin(angRadZ) * Mathf.Cos(angRadZ) - Mathf.Cos(angRadX) * Mathf.Sin(angRadY));
        float y = point.x * Mathf.Cos(angRadZ) * Mathf.Sin(angRadY) + point.y * (Mathf.Cos(angRadX) * Mathf.Cos(angRadY) + Mathf.Sin(angRadX) * Mathf.Sin(angRadZ) * Mathf.Sin(angRadY));
        float z = -point.x * Mathf.Sin(angRadZ) + point.y * Mathf.Sin(angRadX) * Mathf.Cos(angRadZ);
        return new Vector3(x, y, z);
    }
}

/* 
Notes:
basePoints = new Vector3[6];
basePoints[0] = new Vector3(-19.23f, -53.18f, 0.0f);
basePoints[1] = new Vector3(19.23f, -53.18f, 0.0f);
basePoints[2] = new Vector3(55.67f, 9.94f, 0.0f);
basePoints[3] = new Vector3(36.45f, 43.24f, 0.0f);
basePoints[4] = new Vector3(-36.45f, 43.24f, 0.0f);
basePoints[5] = new Vector3(-55.67f, 9.94f, 0.0f);
platformPoints = new Vector3[6];
platformPoints[0] = new Vector3(-33.24f, -26.78f, 0.0f);
platformPoints[1] = new Vector3(33.24f, -26.78f, 0.0f);
platformPoints[2] = new Vector3(39.82f, -15.40f, 0.0f);
platformPoints[3] = new Vector3(6.58f, 42.18f, 0.0f);
platformPoints[4] = new Vector3(-6.58f, 42.18f, 0.0f);
platformPoints[5] = new Vector3(-39.82f, -15.40f, 0.0f);
rodLength = 79.4f // instructor platform
Mathf.Sqrt(sideC * sideC - sideB * sideB)
inputValues[3] * Mathf.Deg2Rad, inputValues[5] * Mathf.Deg2Rad, inputValues[4] * Mathf.Deg2Rad
inputValues[0], inputValues[1], inputValues[2]
platformDefaultHeight
(platformPoints[i] - basePoints[i]).magnitude;
*/