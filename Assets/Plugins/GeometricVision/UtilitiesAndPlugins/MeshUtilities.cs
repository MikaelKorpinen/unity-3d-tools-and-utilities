using UnityEngine;

namespace Plugins.GeometricVision.Utilities
{
    public class MeshUtilities
    {

        public static bool IsInsideFrustum(Vector3 point, UnityEngine.Plane[] planes)
        {
            foreach (var plane in planes)
            {
                if (plane.GetDistanceToPoint(point) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        static bool Equals(GeometryDataModels.Edge x, GeometryDataModels.Edge y)
        {
            var distance1 = Vector3.Distance(x.firstVertex, y.firstVertex);
            var distance2 = Vector3.Distance(x.secondVertex, y.secondVertex);

            if (distance1 < 0.1f && distance2 < 0.1f)
            {
                return true;
            }

            var distance3 = Vector3.Distance(x.firstVertex, y.secondVertex);
            var distance4 = Vector3.Distance(x.secondVertex, y.firstVertex);

            if (distance3 < 0.1f && distance4 < 0.1f)
            {
                return true;
            }

            return false;
        }
    }
}