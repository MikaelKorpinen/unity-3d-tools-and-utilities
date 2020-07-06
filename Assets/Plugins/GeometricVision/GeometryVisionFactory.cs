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
         //   AddDefaultTarget(geoTypes, layerIndex, geoVisionComponent);
            geoVisionManagerGO.transform.position = startingPosition;
            geoVisionManagerGO.transform.rotation = rotation;

            return geoVisionComponent.gameObject;
        }

        /// <summary>
        /// Factory method for building up Geometric vision plugin on a scene via code
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
            
            var geoVisionComponent = CreateGeoVision(new GameObject(), debugModeEnabled);
            var processor = CreateGeometryProcessor(geoVisionManagerGO, geoVisionComponent);

            var head = CreateHead(geoVisionManagerGO, geoVisionComponent, processor);
            CreateEye(geoVisionManagerGO, fieldOfView, geoVisionComponent);
            
            var gameObject = geoVisionComponent.gameObject;
            var transform = gameObject.transform;
            transform.position = startingPosition;
            transform.rotation = rotation;
            AddAdditionalTargets(geoVisionComponent, geoTypes,layerIndex);
            return gameObject;
        }

        private void AddAdditionalTargets(GeometryVision geoVision, List<GeometryType> geoTypes, int layerIndex)
        {
            foreach (var geoType in geoTypes)
            {
                if (geoType == GeometryType.Lines)
                {
                    geoVision.TargetedGeometries.Add(new VisionTarget(GeometryType.Lines,layerIndex,new GeometryLineTargeting()));
                }                
                if (geoType == GeometryType.Objects)
                {
                    geoVision.TargetedGeometries.Add(new VisionTarget(GeometryType.Lines,layerIndex,new GeometryLineTargeting()));
                }
              
                if (geoType == GeometryType.Vertices)
                {
                    geoVision.TargetedGeometries.Add(new VisionTarget(GeometryType.Vertices,layerIndex,new GeometryVertexTargeting()));
                }
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
            geoVisionComponent.GameObjectBasedProcessing.Value = true;
            geoVisionComponent.EntityBasedProcessing.Value = true;
            
            return geoVisionComponent;
        }

        private GeometryVisionHead CreateHead(GameObject geoVisionHead, GeometryVision geoVisionComponent,
            GeometryVisionProcessor processor)
        {
            if (geoVisionHead.GetComponent<GeometryVisionHead>() == null)
            {
                geoVisionHead.AddComponent<GeometryVisionHead>();
            }

            var head = geoVisionHead.GetComponent<GeometryVisionHead>();
            head.AddProcessor(processor);
            if (head.GeoVisions == null)
            {
                head.GeoVisions = new HashSet<GeometryVision>();  
            }
            head.GeoVisions.Add(geoVisionComponent);
            geoVisionComponent.Head = head;
            return geoVisionHead.GetComponent<GeometryVisionHead>();
        }

        internal GeometryVisionEye CreateEye(GameObject geoVisionManager, float fieldOfView, GeometryVision geoVisionComponent)
        {
            if (geoVisionComponent.gameObject.GetComponent<GeometryVisionEye>()==null)
            {
                geoVisionComponent.gameObject.AddComponent<GeometryVisionEye>();
            }

            var eye = geoVisionComponent.gameObject.GetComponent<GeometryVisionEye>();
            eye.Head = geoVisionManager.GetComponent<GeometryVisionHead>();
            eye.Id = new Hash128().ToString();
            eye.GeoVision = geoVisionComponent;
            geoVisionComponent.InitUnityCamera();
            geoVisionComponent.Eyes.Add(geoVisionComponent.gameObject.GetComponent<GeometryVisionEye>());
 
            return geoVisionComponent.gameObject.GetComponent<GeometryVisionEye>();
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