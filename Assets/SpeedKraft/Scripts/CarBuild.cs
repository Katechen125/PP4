using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CarBuild
{
    public CarPart frontWing;
    public CarPart rearWing;
    public CarPart engine;
    public CarPart suspension;
    public CarPart tyres;
    public CarPart brakes;
    public CarPart sidepods;
    public CarPart floor;

    public bool IsComplete()
    {
        return frontWing != null && rearWing != null &&
               engine != null && suspension != null &&
               tyres != null && brakes != null &&
               sidepods != null && floor != null;
    }

    public List<CarPart> GetAllParts()
    {
        return new List<CarPart>
        {
            frontWing, rearWing, engine, suspension,
            tyres, brakes, sidepods, floor
        };
    }

    public void Reset()
    {
        frontWing = rearWing = engine = suspension = null;
        tyres = brakes = sidepods = floor = null;
    }
}