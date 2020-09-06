using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if STEAMVR
using Valve.VR;
#endif
namespace Plugins.GeometricVision.Examples.ObjectPicking
{
    public class PickObjectScript : MonoBehaviour
    {
        public int maxDistance;
        private ObjectPick controls ;
        
        #if STEAMVR
        public SteamVR_Action_Boolean grabPinchAction = SteamVR_Input.GetAction<SteamVR_Action_Boolean>("GrabPinch");
        #endif
        
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
#if STEAMVR
        private void Update()
        {
            if (grabPinchAction.GetStateDown(SteamVR_Input_Sources.RightHand))
            {
                Pick();
            }
        }
#endif
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
            geoVision.TriggerTargetingActions();
            var target = geoVision.GetClosestTarget(false);
            // if distances are zeroed it means there was no targets inside vision area and the system return default
            // target, because target struct cannot be null
            if (target.distanceToCastOrigin > 0)
            {
                if (Vector3.Distance(this.transform.position, target.position) < maxDistance)
                {
                    geoVision.MoveClosestTarget(transform.position, 1.5f);
                }
            }
        }
    }
}
