using UnityEngine;

namespace Plugins.GeometricVision.Examples.ObjectPicking
{
    public class PickingStartingEffect : MonoBehaviour
    {
        private GeometryVision geoVision;

        private new ParticleSystem particleSystem;
        private GeometryDataModels.Target closestTarget;

        [SerializeField, Tooltip("Locks the effect to be spawned to the GeometryVision components transforms position")]
        private bool lockPositionToParent = false;

        private Transform cachedTransform;

        // Start is called before the first frame update
        void Start()
        {
            cachedTransform = transform;
            if (cachedTransform.parent != null)
            {
                geoVision = cachedTransform.parent.GetComponent<GeometryVision>();
            }

            cachedTransform.parent = null;
            particleSystem = GetComponent<ParticleSystem>();
        }

        // Update is called once per frame
        void Update()
        {
            closestTarget = geoVision.GetClosestTarget();
            if (closestTarget.distanceToCastOrigin < 0.56f)
            {
                Destroy(this);
            }

            var shape = particleSystem.shape;
            shape.length = closestTarget.distanceToCastOrigin;
            cachedTransform.LookAt(closestTarget.position);
            if (lockPositionToParent)
            {
                cachedTransform.position = geoVision.transform.position;
            }
        }
    }
}