using System;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Plugins.GeometricVision.Examples.ObjectPicking
{
    public class PickObjectScript : MonoBehaviour
    {
        public int maxDistance;
        private ObjectPick controls ;

        private GeometryVision geoVision;
        // Start is called before the first frame update
        void Awake()
        {
            controls = new ObjectPick();
            controls.Player.Pick.performed += context => Pick();
        }

        private void Start()
        {
            geoVision = GetComponent<GeometryVision>();
        }

        void OnEnable()
        {
            controls.Player.Pick.Enable();

        }

        void OnDisable()
        {
            controls.Player.Pick.Disable();
        }

        void Pick()
        {
            if (Vector3.Distance(this.transform.position, geoVision.GetClosestTarget(false).position) < maxDistance)
            {
                geoVision.MoveClosestTarget(transform.position, 0.5f);
            }
        }

    }
}
