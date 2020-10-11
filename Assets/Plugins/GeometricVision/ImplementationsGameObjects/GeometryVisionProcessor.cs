using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Utilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.SceneManagement;

namespace Plugins.GeometricVision.ImplementationsGameObjects
{
    /// <inheritdoc />
    [DisallowMultipleComponent]
    public class GeometryVisionProcessor : MonoBehaviour, IGeoProcessor
    {
        private int lastCount = 0;
        private string currentTag = "";


        private HashSet<Transform> allTransforms { get; set; }
        public List<GameObject> RootObjects { get; set; }

        public HashSet<Transform> GetTransforms(List<GameObject> objs)
        {
            var result = new HashSet<Transform>();
            GetTransforms(objs, ref result);
            return result;
        }

        public List<Transform> GetAllTransforms()
        {
            return this.allTransforms.ToList();
        }


        // Start is called before the first frame update
        void Awake()
        {
            allTransforms = new HashSet<Transform>();
            RootObjects = new List<GameObject>();
        }

        public int CountSceneObjects()
        {
            SceneManager.GetActiveScene().GetRootGameObjects(RootObjects);
            return CountObjectsInHierarchy(RootObjects);
        }

        /// <summary>
        /// Used to check, if things inside scene has changed. Like if new object has been removed or moved.
        /// This trigger re fetch for desired transforms and objects
        /// </summary>
        public void CheckSceneChanges(GeometryVision geoVision)
        {

                SceneManager.GetActiveScene().GetRootGameObjects(RootObjects);
                var currentObjectCount = CountObjectsInHierarchy(RootObjects);
                
                UpdateSceneChanges(currentObjectCount);
            

            void UpdateSceneChanges(int currentObjectCountIn)
            {
                if (currentObjectCountIn != lastCount || geoVision.TargetingInstructions[0].TargetTag != currentTag)
                {
                    currentTag = geoVision.TargetingInstructions[0].TargetTag;
                    if (geoVision.TargetingInstructions[0].TargetTag.Length == 0)
                    {
                        UpdateSceneTransforms(RootObjects);
                        
                        void UpdateSceneTransforms(List<GameObject> rootObjects)
                        {
                            this.allTransforms=  GetTransformsFromRootObjects(rootObjects, this.allTransforms);
                        }
                    }
                    else
                    {
                        this.allTransforms = new HashSet<Transform>(GameObject
                            .FindGameObjectsWithTag(geoVision.TargetingInstructions[0].TargetTag).ToList()
                            .Select(go => go.transform).ToList());
                    }

                    
                    lastCount = currentObjectCountIn;

                    CreateGeoInfoObjects(allTransforms, geoVision.Runner.GeoMemory.GeoInfos,
                        geoVision.TargetingInstructions, geoVision.CollidersTargeted, geoVision.UseBounds);
                }
            }
        }

        /// <summary>
        /// Goes through all the root objects and counts their children.
        /// </summary>
        /// <param name="rootGameObjects"></param>
        /// <returns></returns>
        public int CountObjectsInHierarchy(List<GameObject> rootGameObjects)
        {
            int numberOfObjects = 0;
            foreach (var root in rootGameObjects)
            {
                numberOfObjects = CountObjectsInTransformHierarchy(root.transform, numberOfObjects + 1);
            }

            return numberOfObjects;
        }

        /// <summary>
        /// recursively count all the transforms in the scene.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="numberOfObjects"></param>
        /// <returns></returns>
        private static int CountObjectsInTransformHierarchy(Transform root, int numberOfObjects)
        {
            int childCount = root.childCount;
            for (var index = 0; index < childCount; index++)
            {
                Transform transform = root.GetChild(index);
                numberOfObjects = CountObjectsInTransformHierarchy(transform, numberOfObjects + 1);
            }

            return numberOfObjects;
        }
   
        /// <summary>
        /// Gets all the transforms from list of root objects
        /// </summary>
        /// <param name="rootObjects"></param>
        /// <param name="targetTransforms"></param>
        /// <returns></returns>
        public HashSet<Transform> GetTransformsFromRootObjects(List<GameObject> rootObjects, HashSet<Transform> targetTransforms)
        {
            int numberOfObjects = 0;

            for (var index = 0; index < rootObjects.Count; index++)
            {
                var root = rootObjects[index];
                targetTransforms.Add(root.transform);
                GetObjectsInTransformHierarchy(root.transform, ref targetTransforms, numberOfObjects + 1);
            }

            return targetTransforms;
        }

        /// <summary>
        /// Gets all the transforms from list of objects
        /// </summary>
        /// <param name="gameObjects"></param>
        /// <param name="targetTransforms"></param>
        /// <returns></returns>
        public void GetTransforms(List<GameObject> gameObjects, ref HashSet<Transform> targetTransforms)
        {
            for (var index = 0; index < gameObjects.Count; index++)
            {
                var root = gameObjects[index];
                targetTransforms.Add(root.transform);
            }
        }

        private static int GetObjectsInTransformHierarchy(Transform root, ref HashSet<Transform> targetList,
            int numberOfObjects)
        {
            int childCount = root.childCount;
            for (var index = 0; index < childCount; index++)
            {
                targetList.Add(root.GetChild(index));
                GetObjectsInTransformHierarchy(root.GetChild(index), ref targetList, numberOfObjects + 1);
            }

            return numberOfObjects;
        }

        /// <summary>
        /// Creates GeoInfo objects and optionally handles copying geometry from Unity Mesh to geoInfo object.
        /// </summary>
        /// <param name="allTransformsIn"></param>
        /// <param name="geoInfos"></param>
        /// <param name="targetedGeometries"></param>
        /// <param name="collidersTargeted"></param>
        /// <param name="requireRenderer"></param>
        private void CreateGeoInfoObjects(HashSet<Transform> allTransformsIn, List<GeometryDataModels.GeoInfo> geoInfos,
            List<TargetingInstruction> targetedGeometries, bool collidersTargeted, bool requireRenderer)
        {
            var aTrans = new HashSet<Transform>(allTransformsIn);
            foreach (var transform in allTransformsIn)
            {
                if (!transform)
                {
                    continue;
                }

                var geoInfo = CreateGeoInfoObject(transform);

                if (requireRenderer)
                {
                    geoInfo = GetGeoInfoGeometryData(targetedGeometries, geoInfo, transform);
                }

                geoInfos.Add(geoInfo);
            }

            allTransformsIn = aTrans;

            GeometryDataModels.GeoInfo CreateGeoInfoObject(Transform seenObject)
            {
                GeometryDataModels.GeoInfo geoInfo = new GeometryDataModels.GeoInfo();
                geoInfo.gameObject = seenObject.gameObject;
                geoInfo.transform = seenObject;

                return geoInfo;
            }

            GeometryDataModels.GeoInfo GetGeoInfoGeometryData(List<TargetingInstruction> targetedGeometries2,
                GeometryDataModels.GeoInfo geoInfo, Transform transform)
            {
                var renderer = transform.GetComponent<Renderer>();
                geoInfo.renderer = renderer;

                if (renderer && GeometryIsTargeted(targetedGeometries2))
                {
                    geoInfo.edges = new GeometryDataModels.Edge[0];

                    var meshFilter = transform.GetComponent<MeshFilter>();
                    if (meshFilter)
                    {
                        geoInfo.mesh = meshFilter.mesh;
                    }
                }
                else
                {
                    geoInfo.edges = new GeometryDataModels.Edge[0];
                }

                return geoInfo;
            }
        }


        /// <summary>
        /// Check if user has selected mesh geometry as target for the operation
        /// </summary>
        /// <param name="targetingInstructions"></param>
        /// <returns></returns>
        private bool GeometryIsTargeted(List<TargetingInstruction> targetingInstructions)
        {
            bool found = false;
            foreach (var targetingInstruction in targetingInstructions)
            {
                if (targetingInstruction.GeometryType == GeometryType.Lines ||
                    targetingInstruction.GeometryType == GeometryType.Vertices)
                {
                    found = true;
                }
            }

            return found;
        }

    }
}