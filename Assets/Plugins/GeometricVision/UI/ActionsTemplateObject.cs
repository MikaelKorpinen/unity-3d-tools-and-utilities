using System.IO;
using System.Security.Permissions;
using GeometricVision;
using UnityEditor;
using UnityEngine;

namespace Plugins.GeometricVision.UI
{
    
    [CustomEditor(typeof(ActionsTemplateObject))]
    internal class ActionsTemplateDrawer : UnityEditor.Editor
    {
        private Texture headerTexture;
        private float textureBottomSpaceExtension = 50f;
        private float ratioMultiplier;
        
        public override void OnInspectorGUI()
        {
            if (headerTexture == null)
            {
                headerTexture = LoadPNG(Application.dataPath+ GeometryVisionSettings.HeaderImagePath);

                Texture2D LoadPNG(string filePath) {
 
                    Texture2D texture2D = null;
                    byte[] fileData;
 
                    if (File.Exists(filePath))     {
                        fileData = File.ReadAllBytes(filePath);
                        texture2D = new Texture2D(2, 2);

                        texture2D.LoadImage(fileData); 


                    }
                    return texture2D;
                }
            }
            
            DrawTexture();
            DrawDefaultInspector ();

            void DrawTexture()
            {
                GUILayout.Label("Geometric vision actions template");
                ratioMultiplier = (float) headerTexture.height / (float) headerTexture.width;
                EditorGUI.DrawPreviewTexture(
                    new Rect(25, 60, EditorGUIUtility.currentViewWidth, EditorGUIUtility.currentViewWidth * ratioMultiplier),
                    headerTexture);
                GUILayout.Space(EditorGUIUtility.currentViewWidth * ratioMultiplier + textureBottomSpaceExtension);
            }
        }
    }
    
    [CreateAssetMenu(fileName = "Actions", menuName = "ScriptableObjects/ActionsForTargeting", order = 1)]
    public class ActionsTemplateObject : ScriptableObject
    {
        [Header("Hand effect Settings")] [SerializeField]
        private bool startActionEnabled;
        [SerializeField, Tooltip("Start delay for instantiation of startActionGameObject")] private float startDelay = 0;
        [SerializeField, Tooltip("Duration/lifeTime for instantiated startActionGameObject")] private float startDuration = 0;
        [SerializeField, Tooltip("Prefab containing animation or visualisation for start effect")] private GameObject startActionObject;

        [Header("Between target and hand effect Settings")] [SerializeField]
        private bool actionEnabled = true;
        [SerializeField, Tooltip("Start delay for instantiation of mainActionGameObject")] private float mainActionDelay = 0;
        [SerializeField, Tooltip("Duration/lifeTime for instantiated mainActionGameObject")] private float mainActionDuration = 0;
        [SerializeField, Tooltip("Prefab containing animation or visualisation for main effect")] private GameObject mainActionObject;

        [Header("Target effect Settings")] [SerializeField]
        private bool endActionEnabled = true;
        [SerializeField, Tooltip("Start delay for instantiation of endActionGameObject")] private float endDelay = 0;
        [SerializeField, Tooltip("Duration/lifeTime for instantiated endActionGameObject")] private float endDuration = 0;
        [SerializeField, Tooltip("Prefab containing animation or visualisation for end effect")] private GameObject endActionObject;

        void OnValidate()
        {
            startDelay = Mathf.Clamp(startDelay, 0, float.MaxValue); 
            mainActionDelay = Mathf.Clamp(mainActionDelay, 0, float.MaxValue); 
            endDelay = Mathf.Clamp(endDelay, 0, float.MaxValue); 
            startDuration = Mathf.Clamp(startDuration, 0, float.MaxValue); 
            mainActionDuration = Mathf.Clamp(mainActionDuration, 0, float.MaxValue); 
            endDuration = Mathf.Clamp(endDuration, 0, float.MaxValue); 
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

        public void OnBeforeSerialize()
        {
            throw new System.NotImplementedException();
        }

        public void OnAfterDeserialize()
        {
            throw new System.NotImplementedException();
        }
    }
}