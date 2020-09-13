using UnityEngine;

namespace Plugins.GeometricVision.Examples.ObjectPicking
{
    public class PickingMiddleEffect : MonoBehaviour
    {
        private GeometryVision geoVision;
        private LineRenderer lineRenderer;
        private GeometryDataModels.Target closestTarget;

        [SerializeField, Tooltip("Locks the effect to be spawned to the GeometryVision components transforms position")]
        private bool lockPositionToParent;

        private Vector3[] positions = new Vector3[10];
        private float time = 0;

        [SerializeField, Tooltip("Frequency of the lightning effect")]
        private float frequency = 0;

        // Start is called before the first frame update
        void Start()
        {
            if (transform.parent != null)
            {
                geoVision = transform.parent.GetComponent<GeometryVision>();
            }

            transform.parent = null;
            lineRenderer = GetComponent<LineRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            closestTarget = geoVision.GetClosestTarget(false);
            if (closestTarget.distanceToCastOrigin < 0.56f)
            {
                Destroy(this);
            }
            var position = geoVision.transform.position;
            positions[0] = position;
            ElectrifyPoints(position, frequency);
            positions[9] = closestTarget.position;
            lineRenderer.SetPositions(positions);
        }

        private void ElectrifyPoints(Vector3 position, float frequency)
        {
            time += Time.deltaTime;
            if (time > frequency)
            {
                time = 0f;
                positions[1] = ElectrifyPoint(position, 0.1f, 3f);
                positions[2] = ElectrifyPoint(position, 0.2f, 0);
                positions[3] = ElectrifyPoint(position, 0.3f, 7.5f);
                positions[4] = ElectrifyPoint(position, 0.4f, 0);
                positions[5] = ElectrifyPoint(position, 0.5f, 12f);
                positions[6] = ElectrifyPoint(position, 0.6f, 0);
                positions[7] = ElectrifyPoint(position, 0.7f, 30f);
                positions[8] = ElectrifyPoint(position, 0.8f, 0);
            }
        }

        private Vector3 ElectrifyPoint(Vector3 position, float pointOffsetFromStartToFinish, float strength)
        {
            var direction = closestTarget.position - position;
            direction = new Vector3(direction.x * pointOffsetFromStartToFinish,
                direction.y * pointOffsetFromStartToFinish, direction.z * pointOffsetFromStartToFinish);
            var middlepoint = direction + position;
            return new Vector3(middlepoint.x, middlepoint.y + Mathf.Sin(Time.deltaTime * strength), middlepoint.z);
        }
    }
}