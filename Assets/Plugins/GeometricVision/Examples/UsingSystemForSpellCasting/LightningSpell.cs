using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
#if STEAMVR
using Valve.VR;
#endif
namespace Plugins.GeometricVision.Examples.UsingSystemForSpellCasting
{
    public class LightningSpell : MonoBehaviour
    {
        [SerializeField] private float maxDistance;
        [SerializeField] private float radius;
        
#if STEAMVR
        public SteamVR_Action_Boolean grabPinchAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
#endif
        private GeometryVision geoVision;

        private void Start()
        {
            geoVision = GetComponent<GeometryVision>();
        }


        private void Update()
        {
#if STEAMVR
            if (grabPinchAction.GetStateUp(SteamVR_Input_Sources.RightHand))
            {
                Cast();
            }
            if (Input.GetMouseButtonUp(0))
            {
                Cast();
            }
#else
            if (Input.GetMouseButtonUp(0))
            {
                Cast();
            }
#endif
        }

        void OnValidate()
        {
            maxDistance = Mathf.Clamp(maxDistance, 0, float.MaxValue);
            radius = Mathf.Clamp(radius, 0, float.MaxValue);
        }

        void Cast()
        {
            var target = geoVision.GetClosestTarget();
            // if distances are zeroed it means there was no targets inside vision area and the system return default
            // target, because target struct cannot be null for it to work with entities

            if (target.distanceToCastOrigin > 0)
            {
                if (Vector3.Distance(this.transform.position, target.projectedTargetPosition) < maxDistance
                    && Vector3.Distance(target.projectedTargetPosition, target.position) < radius)
                {
                    geoVision.TriggerTargetingActions();
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
                Handles.color = Color.green;
                Vector3 resetToVector = Vector3.zero;
                if (geoVision == null)
                {
                    geoVision = GetComponent<GeometryVision>();
                    return;
                }
                var geoVisionTransform = geoVision.transform;
                DrawTargetingVisualIndicators(geoVisionTransform.position, geoVision.ForwardWorldCoordinate,
                    Color.green);

                var position = geoVision.transform.position;
                var forward = position + geoVision.transform.forward * maxDistance;
                var up = geoVisionTransform.up;
                var borderUp = Vector3.Scale(up, new Vector3(radius, radius, radius));
                var right = geoVisionTransform.right;
                var borderRight = Vector3.Scale(right, new Vector3(radius, radius, radius));
                var borderLeft = Vector3.Scale(-right, new Vector3(radius, radius, radius));
                var borderDown = Vector3.Scale(-up, new Vector3(radius, radius, radius));

                DrawTargetingVisualIndicators(forward + borderRight, position + borderRight,
                    Color.green);
                DrawTargetingVisualIndicators(forward + borderLeft, position + borderLeft,
                    Color.green);
                DrawTargetingVisualIndicators(forward + borderUp, position + borderUp, Color.green);
                DrawTargetingVisualIndicators(forward + borderDown, position + borderDown,
                    Color.green);
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

