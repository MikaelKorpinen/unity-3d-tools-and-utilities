using System.Linq;
using Plugins.GeometricVision;
using UnityEngine;

namespace GeometricVision
{
    public class GeometryVisionFactory
    {
        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView,
            GeometryVisionEye eye, GeometryType geoType, int layerIndex)
        {
            GameObject geoVision = GameObject.Find("geoVision");
            if (geoVision == null)
            {
                geoVision = new GameObject("geoVision");
            }
            CreateHead(geoVision);
            geoVision.GetComponent<GeometryVisionHead>().Brain = CreateBrain(geoVision);
            geoVision.GetComponent<GeometryVisionHead>().Eye = eye;
            geoVision.GetComponent<GeometryVisionHead>().Eye.ControllerBrain = geoVision.GetComponent<GeometryVisionHead>().Brain;
            geoVision.transform.position = startingPosition;
            geoVision.transform.rotation = rotation;


            return geoVision;
        }

        public GameObject CreateGeometryVision(Vector3 startingPosition, Quaternion rotation, float fieldOfView, GeometryType geoType, int layerIndex)
        {
            GameObject geoVision = GameObject.Find("geoVision");
            if (geoVision == null)
            {
                geoVision = new GameObject("geoVision");
            }

            CreateHead(geoVision);
            geoVision.GetComponent<GeometryVisionHead>().Brain = CreateBrain(geoVision);
            geoVision.GetComponent<GeometryVisionHead>().Eye = CreateEye(geoVision, fieldOfView);
            geoVision.GetComponent<GeometryVisionHead>().Eye.Head = geoVision.GetComponent<GeometryVisionHead>();
            geoVision.GetComponent<GeometryVisionHead>().Eye.ControllerBrain = geoVision.GetComponent<GeometryVisionHead>().Brain;

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