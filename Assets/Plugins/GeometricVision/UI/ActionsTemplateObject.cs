using UnityEngine;

namespace Plugins.GeometricVision.UI
{
    [CreateAssetMenu(fileName = "Actions", menuName = "ScriptableObjects/ActionsForTargeting", order = 1)]
    public class ActionsTemplateObject : ScriptableObject
    {
        [SerializeField]private  bool startActionEnabled;
        [SerializeField]private float startDelay = 0;
        [SerializeField]private float startDuration = 0;
        [SerializeField]private GameObject startActionObject;
        
        [SerializeField]private bool actionEnabled = true;
        [SerializeField]private float delay = 0;
        [SerializeField]private float duration = 0;
        [SerializeField]private GameObject actionObject;
        
        [SerializeField]private bool endActionEnabled = true;
        [SerializeField]private float endDelay = 0;
        [SerializeField]private float endDuration = 0;
        [SerializeField]private GameObject endActionObject;
        
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

        public bool ActionEnabled
        {
            get { return actionEnabled; }
            set { actionEnabled = value; }
        }

        public bool EndActionEnabled
        {
            get { return endActionEnabled; }
            set { endActionEnabled = value; }
        }

        public float Delay
        {
            get { return delay; }
            set { delay = value; }
        }

        public float Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        public GameObject ActionObject
        {
            get { return actionObject; }
            set { actionObject = value; }
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
