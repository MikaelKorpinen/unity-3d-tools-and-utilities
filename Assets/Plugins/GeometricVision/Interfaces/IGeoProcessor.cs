using System.Collections.Generic;
using UniRx;
using UnityEngine;

namespace Plugins.GeometricVision.Interfaces
{
    public interface IGeoProcessor
    {
    
        /// <summary>
        /// Counts all the scene objects in the current active scene. Not including objects from other scenes.
        /// </summary>
        /// <returns></returns>
        int CountSceneObjects();
        
        /// <summary>
        /// Checks if there are new game objects or entities on the scene and then updates the situation.
        /// Use only if you cant wait for a frame.
        /// </summary>
        /// <param name="geoVision"> GeometryVision component to use for scene checking</param>
        void CheckSceneChanges(GeometryVision geoVision);
 
    }
}
