using System;
using UnityEngine;

namespace Plugins.GeometricVision.Examples.ObjectPicking
{
    public class PickingEndingEffect : MonoBehaviour
    {
        private GeometryVision geoVision;
        private GeometryDataModels.Target closestTarget;

        [SerializeField, Tooltip("Locks the effect to be spawned to the GeometryVision components transforms position")]
        private bool lockPositionToTarget = false;

        private Transform cachedTransform;
        private ParticleSystem.TriggerModule trigger;
        [SerializeField]private bool rotateTowardsCamera = false;
        private void Awake()
        {
             this.GetComponent<ParticleSystem>().Stop();
         
        }

        // Start is called before the first frame update
        void Start()
        {
            cachedTransform = transform;
            if (transform.parent != null)
            {
                geoVision = transform.parent.GetComponent<GeometryVision>();
            }

            transform.parent = null;
            cachedTransform.position = geoVision.GetClosestTarget().position;
            this.GetComponent<ParticleSystem>().Play();
        }

        // Update is called once per frame
        void Update()
        {
            closestTarget = geoVision.GetClosestTarget();
            if (closestTarget.distanceToCastOrigin == 0)
            {
                Destroy(this.gameObject);
            }
            if (closestTarget.distanceToCastOrigin > 0)
            {
                if (lockPositionToTarget)
                {
                    cachedTransform.position = closestTarget.position;
                }

                if (rotateTowardsCamera)
                {
                    transform.LookAt(geoVision.transform.position);
                }
            }
        }
    }
}