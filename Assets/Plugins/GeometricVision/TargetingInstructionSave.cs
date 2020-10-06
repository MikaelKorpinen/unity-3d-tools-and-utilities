using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
#endif

namespace Plugins.GeometricVision
{
    public class TargetingInstructionSave : ScriptableObject
    {
        [SerializeField] private Type entityFilter;
        [SerializeField] private UnityEngine.Object entityFilterObject;

        public Type EntityFilter
        {
            get { return entityFilter; }
            set { entityFilter = value; }
        }

        public Object EntityFilterObject
        {
            get { return entityFilterObject; }
            set { entityFilterObject = value; }
        }
        
    }
}