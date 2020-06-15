using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using UnityEngine;

public class GeometryObjectTargeting : IGeoTargeting
{
    public Vector3 ClosestPointOnRay(Vector3 rayLocation, Vector3 rayDirection, List<GeometryDataModels.GeoInfo> targets)
    {
        foreach (var target in targets)
        {
            Vector3 point =  target.transform.position;
            Vector3 rayDirectionEndPoint = rayDirection;
            point = VectorToRaySpace(rayLocation, point);
            rayDirectionEndPoint = VectorToRaySpace(rayLocation, rayDirection);
            Vector3 projection = Vector3.Project(point, rayDirectionEndPoint);
        }
        return Vector3.back;
    }

    private Vector3 VectorToRaySpace(Vector3 rayLocation,  Vector3 target)
    {
        return target - rayLocation;
    }
}
