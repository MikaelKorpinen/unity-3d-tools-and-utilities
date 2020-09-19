using Plugins.GeometricVision.Utilities;
using UnityEngine;

namespace Plugins.GeometricVision.Examples.ObjectPicking
{
    /// <summary>
    /// Simple example script made to demo what can be done with the system.
    /// Draws an electrified line between target and player/hand
    /// 
    /// </summary>
    public class PickingMiddleEffect : MonoBehaviour
    {
        private GeometryVision geoVision;
        private LineRenderer lineRenderer;
        private GeometryDataModels.Target closestTarget;
        private GeometryDataModels.Target currentTarget;

        private Vector3[] positions = new Vector3[10];
        private float time = 0;

        [SerializeField, Tooltip("Frequency of the lightning effect")]
        private float frequency = 0;        
        [SerializeField, Tooltip("how wide and strong is the effect")]
        private float strengthModifier = 0;

        private float sinTime = 0;

        private Vector3 targetingSystemPosition;

        // Start is called before the first frame update
        void Start()
        {
            GetGeometricVisionFromParentAndUnParent();
            currentTarget = geoVision.GetClosestTarget();
            lineRenderer = GetComponent<LineRenderer>();

            void GetGeometricVisionFromParentAndUnParent()
            {
                if (transform.parent != null)
                {
                    geoVision = transform.parent.GetComponent<GeometryVision>();
                }

                transform.parent = null;
            }
        }

        // Update is called once per frame
        void Update()
        {
            closestTarget = geoVision.GetClosestTarget();
            if (GeometryVisionUtilities.TargetHasNotChanged(closestTarget, currentTarget))
            {
                targetingSystemPosition = geoVision.transform.position;

                positions = ElectrifyPoints(targetingSystemPosition, frequency, closestTarget.position,
                    closestTarget.distanceToCastOrigin);

                lineRenderer.positionCount = positions.Length;
                lineRenderer.SetPositions(positions);
            }
            else
            {
                Destroy(this.gameObject);
            }
        }

        private Vector3[] ElectrifyPoints(Vector3 position, float frequency, Vector3 closestTargetPosition,
            float closestTargetDistanceToCastOrigin)
        {
            time += Time.deltaTime * frequency * frequency * frequency;
            sinTime += Time.deltaTime * frequency * frequency;

            if (time > frequency)
            {
                time = 0f;
                positions[0] = position + new Vector3(Mathf.Sin(time) * 0.1f, Mathf.Sin(time) * 0.1f,
                    Mathf.Sin(time) * 0.1f);
                positions = ElectrifyPointsBetweenStartToEnd(positions, closestTargetDistanceToCastOrigin, position);
                
                positions[9] = closestTargetPosition + new Vector3(Mathf.Sin(sinTime) * 1f, Mathf.Sin(sinTime) * 1f,
                    Mathf.Sin(sinTime) * 1f);
            }

            return positions;

            Vector3[] ElectrifyPointsBetweenStartToEnd(Vector3[] points, float distance, Vector3 geoCameraLocation)
            {
                int breaker = 1;
                for (int index = 1; index < positions.Length - 1; index++)
                {
                    breaker = breaker * -1;
                    points[index] = ElectrifyPoint(geoCameraLocation, index * 0.1f, breaker* index * strengthModifier * (distance * 0.1f),
                        sinTime);
                }
                return points;
            }
        }

        private Vector3 ElectrifyPoint(Vector3 geoCameraPosition, float pointOffsetFromStartToFinish, float strength,
            float sinTime)
        {
            var direction = closestTarget.position - geoCameraPosition;
            
            direction = new Vector3(
                direction.x * pointOffsetFromStartToFinish,
                direction.y * pointOffsetFromStartToFinish, 
                direction.z * pointOffsetFromStartToFinish);
            
            var middlepoint = direction + geoCameraPosition;
            
            return new Vector3(
                middlepoint.x + Mathf.Sin(sinTime) * strength,
                middlepoint.y + Mathf.Sin(sinTime) * strength,
                middlepoint.z + Mathf.Sin(Time.deltaTime * sinTime) * strength);
        }
    }
}