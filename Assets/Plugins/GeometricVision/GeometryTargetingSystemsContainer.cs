﻿using System.Collections;
using System.Collections.Generic;
using Plugins.GeometricVision.Interfaces.Implementations;
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
        [SerializeField]private List<IGeoTargeting> targetingPrograms =new List<IGeoTargeting>();

        public List<IGeoTargeting> TargetingPrograms
        {
            get { return targetingPrograms; }
            set { targetingPrograms = value; }
        }

        private void Reset()
        {
            InitializeTargeting();
        }

        void Start()
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
                TargetingPrograms = new List<IGeoTargeting>();
            }

            if (GetComponent<GeometryVisionEye>() == null)
            {
                gameObject.AddComponent<GeometryVisionEye>();
                eye = GetComponent<GeometryVisionEye>();
            }
        }

        public void AddTargetedGeometry(VisionTarget geometryContainer)
        {
            if (geometryContainer.GeometryType == GeometryType.Objects)
            {
                IGeoTargeting targetingSystem = new GeometryObjectTargeting();
                geometryContainer.TargetingSystem = targetingSystem;
                TargetingPrograms.Add(geometryContainer.TargetingSystem);
            }
            else if (geometryContainer.GeometryType == GeometryType.Lines)
            {
                IGeoTargeting targetingSystem = new GeometryLineTargeting();
                geometryContainer.TargetingSystem = targetingSystem;
                TargetingPrograms.Add(geometryContainer.TargetingSystem);
            }
        }

        public void RemoveTarget(VisionTarget geometryType)
        {
            if (geometryType.TargetingSystem != null)
            {
                TargetingPrograms.Remove(geometryType.TargetingSystem);
            }
        }
    }
}