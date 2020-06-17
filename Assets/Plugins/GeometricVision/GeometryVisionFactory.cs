using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces.Implementations;
using UnityEngine;

namespace GeometricVision
{
    public class GeometryVisionFactory
    {
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView,
            GeometryVisionEye eye, List<GeometryType> geoTypes, int layerIndex)
        {
            GameObject geoVision = GameObject.Find("geoVision");
            if (geoVision == null)
            {
                geoVision = new GameObject("geoVision");
            }

            CreateHead(geoVision);
            geoVision.GetComponent<GeometryVisionHead>().Brain = CreateBrain(geoVision);
            geoVision.GetComponent<GeometryVisionHead>().Eye = eye;
            geoVision.GetComponent<GeometryVisionHead>().Eye.ControllerBrain =
                geoVision.GetComponent<GeometryVisionHead>().Brain;
            geoVision.transform.position = startingPosition;
            geoVision.transform.rotation = rotation;

            return geoVision;
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
        /// <returns></returns>
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView,
            List<GeometryType> geoTypes, int layerIndex)
        {
            GameObject geoVision = GameObject.Find("geoVision");
            if (geoVision == null)
            {
                geoVision = new GameObject("geoVision");
            }

            var head = CreateHead(geoVision);
            head.Brain = CreateBrain(geoVision);
            head.Eye = CreateEye(geoVision, fieldOfView);
            head.Eye.Head = geoVision.GetComponent<GeometryVisionHead>();
            head.Eye.ControllerBrain = geoVision.GetComponent<GeometryVisionHead>().Brain;
            
            foreach (var geoType in geoTypes)
            {
                geoVision.GetComponent<GeometryVisionHead>().Eye.TargetedGeometries
                    .Add(new VisionTarget(geoType, layerIndex, new GeometryObjectTargeting()));
            }

            geoVision.transform.position = startingPosition;
            geoVision.transform.rotation = rotation;

            return geoVision;
        }

        private GeometryVisionHead CreateHead(GameObject geoVision)
        {
            geoVision.AddComponent<GeometryVisionHead>();
            return geoVision.GetComponent<GeometryVisionHead>();
        }

        private GeometryVisionEye CreateEye(GameObject geoVision, float fieldOfView)
        {
            geoVision.AddComponent<GeometryVisionEye>();
            geoVision.AddComponent<Camera>();
            geoVision.GetComponent<GeometryVisionEye>().Camera1 = geoVision.GetComponent<Camera>();
            geoVision.GetComponent<GeometryVisionEye>().RegenerateVisionArea(fieldOfView);
            return geoVision.GetComponent<GeometryVisionEye>();
        }

        private GeometryVisionBrain CreateBrain(GameObject geoVision)
        {
            geoVision.AddComponent<GeometryVisionBrain>();
            return geoVision.GetComponent<GeometryVisionBrain>();
        }
    }
}