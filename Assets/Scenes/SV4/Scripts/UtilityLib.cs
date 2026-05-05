using UnityEngine;

public class UtilityLib : MonoBehaviour
{
    void Start()
    {
        // John Lab
        float topr = 74.15f; float topa = 12.9f;
        float botr = 96.5f; float bota = 37.5f;

        float topCircumradius, topAngle;
        GetRadiusAndOffset(topr, topa, out topCircumradius, out topAngle);
        Vector3[] platformPoints = CalculatePlatformPoints(topCircumradius, topAngle);
        UtilityLib.ConvertV3ToCppSyntax(platformPoints, "platformPoints");

        float botCircumradius, botAngle;
        GetRadiusAndOffset(botr, bota, out botCircumradius, out botAngle);
        Vector3[] basePoints = CalculateBasePoints(botCircumradius, botAngle);
        UtilityLib.ConvertV3ToCppSyntax(basePoints, "basePoints");

        UtilityLib.ConvertV3ToCSharpSyntax(platformPoints, "platformPoints");
        UtilityLib.ConvertV3ToCSharpSyntax(basePoints, "basePoints");
    }

    public static void GetRadiusAndOffset(float sideLengths, float sideSpacing, out float circumradius, out float offsetAngle)
    {
        // calc radius of circumcircle for the equilateral triangle
        circumradius = sideLengths / Mathf.Sqrt(3);
        float circumradSq = circumradius * circumradius; // shorthand squared circumradius
        // law of cosines to get angle C of triangle from SSS calculation
        float cosC = (circumradSq + circumradSq - sideSpacing * sideSpacing) / (2 * circumradSq);
        offsetAngle = (Mathf.Acos(cosC) / 2.0f) * Mathf.Rad2Deg;
    }

    //-------------------------------------------------------------------------
    /// Interpolation
    //-------------------------------------------------------------------------

    public static float Lerp(float a, float b, float t)
    {
        return (a * (1.0f - t)) + (b * t);
        // or return a + (b-a) * t
    }

    public static float SmoothStep(float edge0, float edge1, float x)
    {
        // Scale, bias and saturate x to 0..1 range
        x = Mathf.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
        // Evaluate polynomial
        return x * x * (3 - 2 * x);
    }

    //-------------------------------------------------------------------------
    /// Math Operations
    //-------------------------------------------------------------------------

    public static int Mod(int x, int m)
    {
        return (x % m + m) % m;
    }

    //-------------
    // MapRange
    //-------------
    // This function will map a value from one range of values to a new range of values.
    // A value of 50 in a range from 0 to 100 can be remapped to a range of 10 to 20
    // which would result in a new value of 15.
    // Syntax: MapRange(inputValue, minimumVal, maximumVal, newMinimumVal, newMaximumVal);
    // Example: float myVal = MapRange(50, 0, 100, 10, 20); // Should equal 15
    public static float MapRange(float val, float oldMin, float oldMax, float newMin, float newMax)
    {
        float ratio = (newMax - newMin) / (oldMax - oldMin);
        float newVal = newMin + ratio * (val - oldMin);
        return Mathf.Clamp(newVal, Mathf.Min(newMin, newMax), Mathf.Max(newMin, newMax));
    }

    // An alternate (less flexible) version of MapRange
    //public static float MapRange(float val, float min, float max, float newMin, float newMax)
    //{
    //    return ((val - min) / (max - min) * (newMax - newMin) + newMin);
    //    // or Y = (X-A)/(B-A) * (D-C) + C
    //}

    // Utility function for converting float time to formatted string for time output
    public static string FloatToTimeString(float time, bool showMinutes = true, int numberOfDecimals = 0)
    {
        string output = "";
        if (showMinutes)
        {
            float min = Mathf.Floor(time / 60f);
            string mins = (Mathf.FloorToInt(min) < 10 ? "0" : "") + min.ToString("F0");
            output = mins;
        }

        float sec = time % 60f;
        string secs = (Mathf.FloorToInt(sec) < 10 ? ":0" : ":") + sec.ToString("F" + numberOfDecimals.ToString("F0"));
        output += secs;
        return output;
    }

    public static float GetAngle(Vector3 v1, Vector3 v2)
    {
        float ang = Vector3.Angle(v1, -v2);
        Vector3 cross = Vector3.Cross(v1, -v2);

        if (cross.z > 0)
            ang = 360 - ang;

        return ang;
    }

    public static string ConvertV3ToCppSyntax(Vector3[] vector3Points, string varName)
    {
        string str = varName + " C++ Syntax\n-------------\n{";
        for (int i = 0; i < vector3Points.Length; i++)
        {
            str += "{" + vector3Points[i].x.ToString("F2") + "f," + vector3Points[i].y.ToString("F2") + "f,0.0f}" + (i==5? "};":",") + "\n";
            //str += varName + "[" + i + "] = {" + vector3Points[i].x.ToString("F2") + "f," + vector3Points[i].y.ToString("F2") + "f,0.0f};\n";
        }
        Debug.Log(str);
        return str;
    }

    public static string ConvertV3ToCSharpSyntax(Vector3[] vector3Points, string varName)
    {
        string str = varName + "C# Syntax\n-------------\n";
        for (int i = 0; i < vector3Points.Length; i++)
        {
            str += varName + "[" + i + "] = new Vector3(" + vector3Points[i].x.ToString("F2") + "f," + vector3Points[i].y.ToString("F2") + "f,0.0f);\n";
        }
        Debug.Log(str);
        return str;
    }
    public static Vector3[] CalculateBasePoints(float baseRadius, float baseAngleOffset)
    {
        Vector3[] basePoints = new Vector3[6];
        float[] sideAngles = new float[] { 270, 270, 30, 30, 150, 150 };

        basePoints[0] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (sideAngles[0] - baseAngleOffset)) * baseRadius, Mathf.Sin(Mathf.Deg2Rad * (sideAngles[0] - baseAngleOffset)) * baseRadius, 0.0f);
        basePoints[1] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (sideAngles[1] + baseAngleOffset)) * baseRadius, Mathf.Sin(Mathf.Deg2Rad * (sideAngles[1] + baseAngleOffset)) * baseRadius, 0.0f);
        basePoints[2] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (sideAngles[2] - baseAngleOffset)) * baseRadius, Mathf.Sin(Mathf.Deg2Rad * (sideAngles[2] - baseAngleOffset)) * baseRadius, 0.0f);
        basePoints[3] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (sideAngles[3] + baseAngleOffset)) * baseRadius, Mathf.Sin(Mathf.Deg2Rad * (sideAngles[3] + baseAngleOffset)) * baseRadius, 0.0f);
        basePoints[4] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (sideAngles[4] - baseAngleOffset)) * baseRadius, Mathf.Sin(Mathf.Deg2Rad * (sideAngles[4] - baseAngleOffset)) * baseRadius, 0.0f);
        basePoints[5] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (sideAngles[5] + baseAngleOffset)) * baseRadius, Mathf.Sin(Mathf.Deg2Rad * (sideAngles[5] + baseAngleOffset)) * baseRadius, 0.0f);

        //string str = "basePoints";
        //for (int i = 0; i < 6; i++)
        //{
        //    str += basePoints[i].ToString("F2") + "\n";
        //}
        //Debug.Log(str);

        return basePoints;
    }

    public static Vector3[] CalculatePlatformPoints(float platformRadius, float platformAngleOffset)
    {
        Vector3[] platformPoints = new Vector3[6];
        float[] platformAngles = new float[] { 210, 330, 330, 90, 90, 210 };

        platformPoints[0] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (platformAngles[0] + platformAngleOffset)) * platformRadius, Mathf.Sin(Mathf.Deg2Rad * (platformAngles[0] + platformAngleOffset)) * platformRadius, 0.0f);
        platformPoints[1] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (platformAngles[1] - platformAngleOffset)) * platformRadius, Mathf.Sin(Mathf.Deg2Rad * (platformAngles[1] - platformAngleOffset)) * platformRadius, 0.0f);
        platformPoints[2] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (platformAngles[2] + platformAngleOffset)) * platformRadius, Mathf.Sin(Mathf.Deg2Rad * (platformAngles[2] + platformAngleOffset)) * platformRadius, 0.0f);
        platformPoints[3] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (platformAngles[3] - platformAngleOffset)) * platformRadius, Mathf.Sin(Mathf.Deg2Rad * (platformAngles[3] - platformAngleOffset)) * platformRadius, 0.0f);
        platformPoints[4] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (platformAngles[4] + platformAngleOffset)) * platformRadius, Mathf.Sin(Mathf.Deg2Rad * (platformAngles[4] + platformAngleOffset)) * platformRadius, 0.0f);
        platformPoints[5] = new Vector3(Mathf.Cos(Mathf.Deg2Rad * (platformAngles[5] - platformAngleOffset)) * platformRadius, Mathf.Sin(Mathf.Deg2Rad * (platformAngles[5] - platformAngleOffset)) * platformRadius, 0.0f);

        //string str = "platformPoints";
        //for (int i = 0; i < 6; i++)
        //{
        //    str += platformPoints[i].ToString("F2") + "\n";
        //}
        //Debug.Log(str);

        return platformPoints;
    }
}