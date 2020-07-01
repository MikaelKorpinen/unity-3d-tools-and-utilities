using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Interfaces.Implementations;
using UnityEngine;

namespace Plugins.GeometricVision
{
    public class GeometryVisionFactory
    {
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView,
            GeometryVision geoVisionComponent, List<GeometryType> geoTypes, int layerIndex)
        {
            var geoVisionManagerGO = CreateGeovisionManagerGameObject();
            var processor = CreateGeometryProcessor(geoVisionManagerGO, geoVisionComponent);
            CreateHead(geoVisionManagerGO, geoVisionComponent, processor);
            CreateEye(geoVisionManagerGO, fieldOfView, geoVisionComponent);
            AddDefaultTarget(geoTypes, layerIndex, geoVisionComponent);
            geoVisionManagerGO.transform.position = startingPosition;
            geoVisionManagerGO.transform.rotation = rotation;

            return geoVisionManagerGO;
        }

        /// <summary>
        /// Factory method for building up Geometric vision plugin on a scene
        /// </summary>
        /// <remarks>By default GeometryType to use is objects since plugin needs that in order to work</remarks>
        /// <param name="startingPosition"></param>
        /// <param name="rotation"></param>
        /// <param name="fieldOfView"></param>
        /// <param name="geoTypes"></param>
        /// <param name="layerIndex"></param>
        /// <param name="debugModeEnabled"></param>
        /// <returns></returns>
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView,
            List<GeometryType> geoTypes, int layerIndex, bool debugModeEnabled)
        {
            var geoVisionManagerGO = CreateGeovisionManagerGameObject();
            var geoVisionComponent = CreateGeoVision(geoVisionManagerGO, debugModeEnabled);
            var processor = CreateGeometryProcessor(geoVisionManagerGO, geoVisionComponent);

            var head = CreateHead(geoVisionManagerGO, geoVisionComponent, processor);
            CreateEye(geoVisionManagerGO, fieldOfView, geoVisionComponent);
            AddDefaultTarget(geoTypes, layerIndex, geoVisionComponent);

            geoVisionManagerGO.transform.position = startingPosition;
            geoVisionManagerGO.transform.rotation = rotation;

            return geoVisionManagerGO;
        }

        private static void AddDefaultTarget(List<GeometryType> geoTypes, int layerIndex, GeometryVision geometryVision)
        {
            foreach (var geoType in geoTypes)
            {
                geometryVision.TargetedGeometries
                    .Add(new VisionTarget(geoType, layerIndex, new GeometryObjectTargeting()));
            }
        }

        private static GameObject CreateGeovisionManagerGameObject()
        {
            GameObject geoVision = GameObject.Find("geoVision");
            if (geoVision == null)
            {
                geoVision = new GameObject("geoVision");
            }

            return geoVision;
        }

        private static GeometryVision CreateGeoVision(GameObject geoVision, bool debugModeEnabled)
        {
            geoVision.AddComponent<GeometryVision>();
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            geoVisionComponent.Id = new Hash128().ToString();
            geoVisionComponent.DebugMode = debugModeEnabled;
            return geoVisionComponent;
        }

        private GeometryVisionHead CreateHead(GameObject geoVision, GeometryVision geoVisionComponent,
            GeometryVisionProcessor processor)
        {
            if (geoVision.GetComponent<GeometryVisionHead>() == null)
            {
                geoVision.AddComponent<GeometryVisionHead>();
            }

            var head = geoVision.GetComponent<GeometryVisionHead>();
            head.AddProcessor(processor);
            if (head.GeoVisions == null)
            {
                head.GeoVisions = new HashSet<GeometryVision>();  
            }
            head.GeoVisions.Add(geoVisionComponent);
            geoVisionComponent.Head = head;
            return geoVision.GetComponent<GeometryVisionHead>();
        }

        private GeometryVisionEye CreateEye(GameObject geoVision, float fieldOfView, GeometryVision geoVisionComponent)
        {
            
            geoVision.AddComponent<GeometryVisionEye>();
            var eye = geoVision.GetComponent<GeometryVisionEye>();
            eye.Head = geoVision.GetComponent<GeometryVisionHead>();
            eye.Id = new Hash128().ToString();
            geoVisionComponent.InitUnityCamera();
            geoVisionComponent.Eyes.Add(geoVision.GetComponent<GeometryVisionEye>());

            eye.ControllerProcessor = eye.ControllerProcessor;
            
            return geoVision.GetComponent<GeometryVisionEye>();
        }

        private GeometryVisionProcessor CreateGeometryProcessor(GameObject geoVisionManager,
            GeometryVision geoVisionComponent)
        {
            if (geoVisionManager.GetComponent<GeometryVisionProcessor>() == null)
            {
                geoVisionManager.AddComponent<GeometryVisionProcessor>();
            }
            
            return geoVisionManager.GetComponent<GeometryVisionProcessor>();
        }
    }
}