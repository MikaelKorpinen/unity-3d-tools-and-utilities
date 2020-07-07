using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision;
using UnityEngine;

public interface IGeoTargeting
{
    List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets);
    GeometryType TargetedType { get; }
}
