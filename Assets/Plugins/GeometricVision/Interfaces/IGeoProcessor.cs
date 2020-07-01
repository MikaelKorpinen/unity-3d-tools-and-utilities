using System.Collections.Generic;
using GeometricVision;
using UniRx;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces
{
    public interface IGeoProcessor
    {
        /// <summary>
        /// Contains seen information about geometry objects that are seen by eyes/cameras
        /// </summary>
        /// <returns></returns>
        List<GeometryDataModels.GeoInfo> GeoInfos();
    
        /// <summary>
        /// Counts all the scene objects in the current active scene. Not including objects on Dont destroy on load atm.
        /// </summary>
        /// <returns></returns>
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
        /// <param name="geoVision"></param>
        void CheckSceneChanges(GeometryVision geoVision);

        void Debug(GeometryVision geoVisions);
    }
}
