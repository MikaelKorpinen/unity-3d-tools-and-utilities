using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision;
using UnityEngine;

public interface IGeoBrain
{
    //Contains seen information about geometry objects that are seen by eyes/cameras
    List<GeometryDataModels.GeoInfo> GeoInfos();
    int CountSceneObjects();
    /// <summary>
    /// Gets all the transforms from list of objects
    /// </summary>
    /// <param name="rootObjects"></param>
    /// <param name="targetTransforms"></param>
    /// <returns></returns>
    HashSet<Transform> GetTransforms(List<GameObject> objs);
    List<Transform> GetAllObjects();
    
    /// <summary>
    /// Ask the manager brain to update it knowledge about targeted geometries
    /// </summary>
    /// <param name="targetedGeometries"></param>
    void CheckSceneChanges(List<VisionTarget> targetedGeometries);
}
