using System;
using UnityEngine;
#if UNITY_EDITOR
#endif

namespace Plugins.GeometricVision
{
    [CreateAssetMenu(fileName = "Actions", menuName = "ScriptableObjects/GeometricVision/ActionsForTargeting", order = 1)]
    public class ActionsTemplateObject : ScriptableObject
    {
        [Header("Hand effect Settings")]
        private bool startActionEnabled = false;
        [SerializeField, Tooltip("Start delay for instantiation of startActionGameObject")] private float startDelay = 0;
        [SerializeField, Tooltip("Duration/lifeTime for instantiated startActionGameObject")] private float startDuration = 0;
        [SerializeField, Tooltip("Prefab containing animation or visualisation for start effect")] private GameObject startActionObject;

        [Header("Between target and hand effect Settings")]
        private bool mainActionEnabled = false;
        [SerializeField, Tooltip("Start delay for instantiation of mainActionGameObject")] private float mainActionDelay = 0;
        [SerializeField, Tooltip("Duration/lifeTime for instantiated mainActionGameObject")] private float mainActionDuration = 0;
        [SerializeField, Tooltip("Prefab containing animation or visualisation for main effect")] private GameObject mainActionObject;

        [Header("Target effect Settings")] 
        private bool endActionEnabled = false;
        [SerializeField, Tooltip("Start delay for instantiation of endActionGameObject")] private float endDelay = 0;
        [SerializeField, Tooltip("Duration/lifeTime for instantiated endActionGameObject")] private float endDuration = 0;
        [SerializeField, Tooltip("Prefab containing animation or visualisation for end effect")] private GameObject endActionObject;

        private void Awake()
        {            
            StartActionEnabled = startActionObject;
            MainActionEnabled = mainActionObject;
            EndActionEnabled = endActionObject;
        }

        void OnValidate()
        {
            startDelay = Mathf.Clamp(startDelay, 0, float.MaxValue); 
            mainActionDelay = Mathf.Clamp(mainActionDelay, 0, float.MaxValue); 
            endDelay = Mathf.Clamp(endDelay, 0, float.MaxValue); 
            startDuration = Mathf.Clamp(startDuration, 0, float.MaxValue); 
            mainActionDuration = Mathf.Clamp(mainActionDuration, 0, float.MaxValue); 
            endDuration = Mathf.Clamp(endDuration, 0, float.MaxValue);
            
            StartActionEnabled = startActionObject;
            MainActionEnabled = mainActionObject;
            EndActionEnabled = endActionObject;
        }
        public float StartDelay
        {
            get { return startDelay; }
            set { startDelay = value; }
        }

        public bool StartActionEnabled
        {
            get { return startActionEnabled; }
            set { startActionEnabled = value; }
        }

        public float StartDuration
        {
            get { return startDuration; }
            set { startDuration = value; }
        }

        public GameObject StartActionObject
        {
            get { return startActionObject; }
            set { startActionObject = value; }
        }

        public bool MainActionEnabled
        {
            get { return mainActionEnabled; }
            set { mainActionEnabled = value; }
        }

        public bool EndActionEnabled
        {
            get { return endActionEnabled; }
            set { endActionEnabled = value; }
        }

        public float MainActionDelay
        {
            get { return mainActionDelay; }
            set { mainActionDelay = value; }
        }

        public float MainActionDuration
        {
            get { return mainActionDuration; }
            set { mainActionDuration = value; }
        }

        public GameObject MainActionObject
        {
            get { return mainActionObject; }
            set { mainActionObject = value; }
        }

        public float EndDelay
        {
            get { return endDelay; }
            set { endDelay = value; }
        }

        public float EndDuration
        {
            get { return endDuration; }
            set { endDuration = value; }
        }

        public GameObject EndActionObject
        {
            get { return endActionObject; }
            set { endActionObject = value; }
        }


    }
    
}