using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision;
using UnityEngine;

/// <summary>
/// Made to handle targeting logic.
/// Usage: For new targeting behavior implement this interface and add it to the targeting systems list on the
/// GeometryTargetingSystemsContainer component from GeometricVision component.
/// </summary>
public interface IGeoTargeting
{
    /// <summary>
    /// Gets targets sorted by which target is the closest to the looking direction of the object the GeometricVision
    /// component is added.
    /// </summary>
    /// <param name="rayLocation"></param>
    /// <param name="rayDirection"></param>
    /// <param name="targets"></param>
    /// <returns></returns>
    List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets);
    GeometryType TargetedType { get; }
}
