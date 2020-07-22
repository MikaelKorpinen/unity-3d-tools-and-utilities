using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces.Implementations;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Plugins.GeometricVision
{
    /// <summary>
    /// Made to contain targeting systems created by the user. Has logic for adding and removing targeting systems.
    /// Also handles things like initialization and cleaning up components if removed.
    /// Usage: Component is added and managed automatically by GeometricVision component
    /// </summary>
    [DisallowMultipleComponent]
    public class GeometryTargetingSystemsContainer : MonoBehaviour
    {
        [SerializeField] private bool debugMode;
        private GeometryVisionEye eye;
        [SerializeField]private HashSet<IGeoTargeting> targetingPrograms =new HashSet<IGeoTargeting>();

        public HashSet<IGeoTargeting> TargetingPrograms
        {
            get { return targetingPrograms; }
            set { targetingPrograms = value; }
        }

        private void Reset()
        {
            InitializeTargeting();
        }

        void Awake()
        {
            InitializeTargeting();
        }
        
        public IEnumerator RemoveAddedComponents()
        {
            if (this != null)
            {
                if (this.GetComponent<Camera>() != null)
                {
                    DestroyImmediate(this.GetComponent<Camera>());
                }
            
                if (this.GetComponent<GeometryVisionEye>() != null)
                {
                    DestroyImmediate(GetComponent<GeometryVisionEye>());
                }

                if (this.GetComponent<GeometryTargetingSystemsContainer>() != null)
                {
                    DestroyImmediate(GetComponent<GeometryTargetingSystemsContainer>());
                }
            }

            yield return null;
        }

        
        private void InitializeTargeting()
        {
            if (TargetingPrograms == null)
            {
                TargetingPrograms = new HashSet<IGeoTargeting>();
            }

            if (GetComponent<GeometryVisionEye>() == null)
            {
                gameObject.AddComponent<GeometryVisionEye>();
                eye = GetComponent<GeometryVisionEye>();
            }
        }

        public void AddTargetingProgram(VisionTarget targetingInstructions)
        {
            TargetingPrograms.Add(targetingInstructions.TargetingSystemGameObjects);
        }

        public void RemoveTargetingProgram(VisionTarget geometryType)
        {
            if (geometryType.TargetingSystemGameObjects != null)
            {
                TargetingPrograms.Remove(geometryType.TargetingSystemGameObjects);
            }
        }
    }
}