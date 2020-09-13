using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.Examples.CustomScriptTargetIndicators
{
    public class TargetingIndicator : MonoBehaviour
    {
        private GeometryVision geoVision;
        [SerializeField] private GameObject targetingIndicator;
        private GameObject spawnedTargetingIndicator;
        [SerializeField] private float maxDistance;
        [SerializeField] private float radius;
        // Start is called before the first frame update
        void Start()
        {
            geoVision = GetComponent<GeometryVision>();
            spawnedTargetingIndicator = GameObject.Instantiate(targetingIndicator);
        }

        // Update is called once per frame
        void Update()
        {
            var target = geoVision.GetClosestTarget(false);
            if (target.distanceToCastOrigin > 0)
            {
                if (Vector3.Distance(this.transform.position, target.projectedTargetPosition) < maxDistance
                    && Vector3.Distance(target.projectedTargetPosition, target.position) < radius)
                {
                    spawnedTargetingIndicator.transform.position = target.position;
                    spawnedTargetingIndicator.transform.LookAt(this.transform);
                }
                else
                {
                    //move targeting cursor out of sight
                    spawnedTargetingIndicator.transform.position = new Vector3(0f,-100f,0f);
                }
            }
        }
        
#if UNITY_EDITOR

        /// <summary>
        /// Used for debugging geometry vision and is responsible for drawing debugging info from the data providid by
        /// GeometryVision plugin
        /// </summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            if (geoVision == null)
            {
                geoVision = GetComponent<GeometryVision>();
            }

            if (Selection.activeTransform == this.transform)
            {
                DrawHelper();
            }


            void DrawHelper()
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = Color.blue;
                Vector3 resetToVector = Vector3.zero;
                var geoVisionTransform = geoVision.transform;
                DrawTargetingVisualIndicators(geoVisionTransform.position, geoVision.ForwardWorldCoordinate,
                    Color.blue);

                var position = geoVision.transform.position;
                var forward = position + geoVision.transform.forward * maxDistance;
                var up = geoVisionTransform.up;
                var borderUp = Vector3.Scale(up, new Vector3(radius, radius, radius));
                var right = geoVisionTransform.right;
                var borderRight = Vector3.Scale(right, new Vector3(radius, radius, radius));
                var borderLeft = Vector3.Scale(-right, new Vector3(radius, radius, radius));
                var borderDown = Vector3.Scale(-up, new Vector3(radius, radius, radius));

                DrawTargetingVisualIndicators(forward + borderRight, position + borderRight,
                    Color.blue);
                DrawTargetingVisualIndicators(forward + borderLeft, position + borderLeft,
                    Color.blue);
                DrawTargetingVisualIndicators(forward + borderUp, position + borderUp, Color.green);
                DrawTargetingVisualIndicators(forward + borderDown, position + borderDown,
                    Color.blue);
                Handles.DrawWireArc(forward, geoVisionTransform.forward, right, 360, radius);
                Handles.DrawWireArc(position, geoVisionTransform.forward, right, 360, radius);

                void DrawTargetingVisualIndicators(Vector3 spherePosition, Vector3 lineStartPosition, Color color)
                {
                    Gizmos.color = color;
                    Gizmos.DrawLine(lineStartPosition, spherePosition);
                }
            }
        }
#endif
    }
}