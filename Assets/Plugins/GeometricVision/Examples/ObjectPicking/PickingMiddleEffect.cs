using Plugins.GeometricVision.Utilities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

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
        [SerializeField] private float strengthModifier1 = 0.05f;
        [Range(-10.0f, 10.0f)]
        [SerializeField]private float strengthModifier2 = 0;

        private float sinTime = 0;

        private Vector3 targetingSystemPosition;

        [SerializeField] private ParticleSystem targetParticlesEffect = null;

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
            if (currentTarget.distanceToCastOrigin == 0)
            {
                Destroy(this.gameObject);
            }

            else if (GeometryVisionUtilities.TargetHasNotChanged(closestTarget, currentTarget))
            {
                targetingSystemPosition = geoVision.transform.position;
                PlayParticleSystemAtTarget();

                positions = ElectrifyPoints(targetingSystemPosition, frequency, currentTarget.position,
                    currentTarget.distanceToCastOrigin);

                lineRenderer.positionCount = positions.Length;
                lineRenderer.SetPositions(positions);
            }
            else
            {
                Destroy(this.gameObject);
            }

            void PlayParticleSystemAtTarget()
            {
                if (targetParticlesEffect)
                {
                    targetParticlesEffect.Play(true);
                    targetParticlesEffect.transform.position = currentTarget.position;
                }
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
                    float driver = index /(positions.Length+1f);
                    points[index] = ElectrifyPoint(geoCameraLocation, driver,
                        breaker * index * strengthModifier * (distance * strengthModifier1),
                        sinTime, Random.Range(-strengthModifier2 * driver * 3, strengthModifier2 * driver * 2));
                }

                return points;
            }
        }

        private Vector3 ElectrifyPoint(float3 geoCameraPosition, float pointOffsetFromStartToFinish, float strength,
            float sinTime, float random)
        {
            var direction = closestTarget.position - geoCameraPosition;

            direction = new Vector3(
                direction.x * pointOffsetFromStartToFinish + Random.Range(-0.1f, 0.1f),
                direction.y * pointOffsetFromStartToFinish + Random.Range(-0.1f, 0.1f),
                direction.z * pointOffsetFromStartToFinish + Random.Range(-0.1f, 0.1f));

            var middlepoint = direction + geoCameraPosition;

            return new Vector3(
                middlepoint.x + Mathf.Sin(sinTime) * strength * 2 + random,
                middlepoint.y + Mathf.Sin(sinTime) * strength,
                middlepoint.z + Mathf.Sin(sinTime) * strength * 2 + random);
        }
    }
}