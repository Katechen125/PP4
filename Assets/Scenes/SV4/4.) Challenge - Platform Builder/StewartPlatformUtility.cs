using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StewartPlatformUtility : MonoBehaviour
{
    [SerializeField] float platformSideLength; // top triangle side length
    [SerializeField] float platformPointSpacing; // top spacing between points
    [SerializeField] float baseSideLength; // bottom triangle side length
    [SerializeField] float basePointSpacing; // bottom spacing between points

    void Start()
    {
        Vector3[] platformPoints = CalcualteTopPoints(platformSideLength, platformPointSpacing);
        Vector3[] basePoints = CalcualteBasePoints(baseSideLength, basePointSpacing);

        string outputForFile = "";
        //for (int i = 0; i < platformPoints.Length; i++)
        {
            outputForFile += UtilityLib.ConvertV3ToCppSyntax(platformPoints, "PlatformPoints:") + "\n";
            outputForFile += UtilityLib.ConvertV3ToCSharpSyntax(platformPoints, "PlatformPoints:") + "\n";
            //outputForFile += platformPoints[i].ToString("F2") + "\n";
        }
        //outputForFile += "\nBasePoints:\n";
        //for (int i = 0; i < platformPoints.Length; i++)
        {
            outputForFile += UtilityLib.ConvertV3ToCppSyntax(basePoints, "BasePoints:") + "\n";
            outputForFile += UtilityLib.ConvertV3ToCSharpSyntax(basePoints, "BasePoints:") + "\n";
            //outputForFile += platformPoints[i].ToString("F2") + "\n";
        }

        System.IO.File.WriteAllText(Application.dataPath + "/Platform Measurements.txt", outputForFile);
    }

    public Vector3[] CalcualteBasePoints(float sideLength, float spacing)
    {
        float circumradius, angle;
        UtilityLib.GetRadiusAndOffset(sideLength, spacing, out circumradius, out angle);
        Vector3[] points = UtilityLib.CalculateBasePoints(circumradius, angle);

        return points;
    }

    public Vector3[] CalcualteTopPoints(float sideLength, float spacing)
    {
        float circumradius, angle;
        UtilityLib.GetRadiusAndOffset(sideLength, spacing, out circumradius, out angle);
        Vector3[] points = UtilityLib.CalculatePlatformPoints(circumradius, angle);
        return points;
    }
}
