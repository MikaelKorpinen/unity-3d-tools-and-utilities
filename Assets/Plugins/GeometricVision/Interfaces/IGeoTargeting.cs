using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using UnityEngine;

public interface IGeoTargeting
{
    Vector3 ClosestPointOnRay(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets);
    GeometryType TargetedType { get; }
}
