using Plugins.GeometricVision.EntityScripts.Components;
using Unity.Entities;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.Examples.CustomScriptTargetIndicators
{
    /// <summary>
    /// Example script with purpose to show on how the targeting data could be used to create a targeting indicator.
    /// </summary>
    public class TargetingIndicator : MonoBehaviour
    {
        private GeometryVision geoVision;
        [SerializeField] private GameObject targetingIndicator = null;
        private GameObject spawnedTargetingIndicator;
        [SerializeField] private float maxDistance =0;
        [SerializeField] private float radius=0;
        private TextMesh text;

        // Start is called before the first frame update
        void Start()
        {
            geoVision = GetComponent<GeometryVision>();
            spawnedTargetingIndicator = GameObject.Instantiate(targetingIndicator);
            text = spawnedTargetingIndicator.GetComponentInChildren<TextMesh>();
        }

        // Update is called once per frame
        void Update()
        {
            if (geoVision == null)
            {
                geoVision = GetComponent<GeometryVision>();
                return;
            }
            var target = geoVision.GetClosestTarget();

            if (target.isEntity)
            {
                AssignEntityName();
                
                void AssignEntityName()
                {
                    if (World.DefaultGameObjectInjectionWorld.EntityManager.HasComponent<Name>(target.entity))
                    {
                        text.text = World.DefaultGameObjectInjectionWorld.EntityManager
                            .GetComponentData<Name>(target.entity).Value.ToString();
                    }
                    else
                    {
                        text.text = "Unknown";
                    }
                }
            }
            else
            {
                AssignGameObjectsName();
                
                void AssignGameObjectsName()
                {
                    var go = geoVision.GetGeoInfoBasedOnHashCode(target.GeoInfoHashCode);
                    if (go.gameObject)
                    {
                        if (text != null)
                        {
                            text.text = geoVision.GetGeoInfoBasedOnHashCode(target.GeoInfoHashCode).gameObject.name;
                        }
                    }
                }
            }

            AnimateSimpleTargetIndicator();

            void AnimateSimpleTargetIndicator()
            {
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
                        spawnedTargetingIndicator.transform.position = new Vector3(0f, -100f, 0f);
                    }
                }
            }

            CleanUpTargetingIndicatorWhenThereAreNoTargets();

            void CleanUpTargetingIndicatorWhenThereAreNoTargets()
            {
                if (geoVision.GetClosestTargetCount() == 0)
                {
                    //move targeting cursor out of sight
                    spawnedTargetingIndicator.transform.position = new Vector3(0f, -100f, 0f);
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
            if (this.geoVision == null)
            {
                this.geoVision = GetComponent<GeometryVision>();
                return;
            }

            if (Selection.activeTransform == this.transform)
            {
                DrawHelper();
            }


            void DrawHelper()
            {
                Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
                Handles.color = Color.blue;
                var geoVisionTransform = geoVision.transform;
                var position = geoVisionTransform.position;
                DrawTargetingVisualIndicators(position, geoVision.ForwardWorldCoordinate,
                    Color.blue);


                var forward = position + geoVisionTransform.forward * maxDistance;
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