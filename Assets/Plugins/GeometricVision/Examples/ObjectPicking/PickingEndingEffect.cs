using System;
using UnityEngine;

namespace Plugins.GeometricVision.Examples.ObjectPicking
{
    public class PickingEndingEffect : MonoBehaviour
    {
        private GeometryVision geoVision;

        private ParticleSystem particleSystem;
        private GeometryDataModels.Target closestTarget;

        [SerializeField, Tooltip("Locks the effect to be spawned to the GeometryVision components transforms position")]
        private bool lockPositionToTarget;

        private Transform cachedTransform;
        
        // Start is called before the first frame update
        void Start()
        {
            cachedTransform = transform;
            if (transform.parent != null)
            {
                geoVision = transform.parent.GetComponent<GeometryVision>();
            }

            transform.parent = null;

            particleSystem = GetComponent<ParticleSystem>();
        }

        // Update is called once per frame
        void Update()
        {
            closestTarget = geoVision.GetClosestTarget(false);
            if (closestTarget.distanceToCastOrigin > 0)
            {
                if (closestTarget.distanceToCastOrigin < 0.56f)
                {
                    Destroy(this);
                }

                cachedTransform.LookAt(geoVision.transform);
                if (lockPositionToTarget)
                {
                    cachedTransform.position = closestTarget.position;
                }
            }
        }
    }
}