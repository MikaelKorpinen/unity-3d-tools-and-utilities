using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using UnityEngine;

public interface IGeoBrain
{
    List<GeometryDataModels.GeoInfo> GeoInfos();
    int CountSceneObjects();
    HashSet<Transform> GetTransforms(List<GameObject> objs);
    List<Transform> getAllObjects();
}
