using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces
{
    /// <summary>
    /// Made to handle targeting logic.
    /// Usage: For new targeting behavior implement this interface and add it to the targeting systems list on the
    /// GeometryTargetingSystemsContainer component from GeometricVision component.
    /// </summary>
    public interface IGeoTargeting
    {
        /// <summary>
        /// Gets targeting data
        /// component is added.
        /// </summary>
        /// <param name="rayLocation"></param>
        /// <param name="rayDirection"></param>
        /// <param name="geometryVision"></param>
        /// <param name="targetingInstruction"></param>
        /// <returns></returns>
        NativeArray<GeometryDataModels.Target> GetTargetsAsNativeArray(Vector3 rayLocation, Vector3 rayDirection,GeometryVision geometryVision, TargetingInstruction targetingInstruction);

        /// <summary>
        /// Gets targeting data
        /// </summary>
        /// <param name="rayLocation"></param>
        /// <param name="rayDirection"></param>
        /// <param name="geometryVision"></param>
        /// <param name="targetingInstruction"></param>
        /// <returns></returns>
        List<GeometryDataModels.Target> GetTargets(Vector3 rayLocation, Vector3 rayDirection, GeometryVision geometryVision, TargetingInstruction targetingInstruction);
        GeometryType TargetedType { get; }
        
        /// <summary>
        /// Helper for checking if is targeting system for entities
        /// </summary>
        /// <returns></returns>
        bool IsForEntities();
    }
}
