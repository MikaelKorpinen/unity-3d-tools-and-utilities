using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugins.GeometricVision.ImplementationsGameObjects
{
    /// <inheritdoc />
    [DisallowMultipleComponent]
    public class GeometryVisionProcessor : MonoBehaviour, IGeoProcessor
    {
        [SerializeField] private int lastCount = 0;


        public HashSet<Transform> AllTransforms { get; set; }
        public List<GameObject> RootObjects { get; set; }

        public HashSet<Transform> GetTransforms(List<GameObject> objs)
        {
            var result = new HashSet<Transform>();
            GetTransforms(objs, ref result);
            return result;
        }

        public List<Transform> GetAllObjects()
        {
            return AllTransforms.ToList();
        }


        // Start is called before the first frame update
        void Awake()
        {
            AllTransforms = new HashSet<Transform>();
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
            
            if (currentObjectCount != lastCount)
            {
                lastCount = currentObjectCount;
                UpdateSceneObjects(RootObjects, AllTransforms, "");
                ExtractGeometry(AllTransforms, geoVision.Head.GeoMemory.GeoInfos, geoVision.TargetingInstructions,
                    false);
            }
        }

        public void Debug(GeometryVision geoVisions)
        {
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
        public void GetTransformsFromRootObjects(List<GameObject> rootObjects, ref HashSet<Transform> targetTransforms)
        {
            int numberOfObjects = 0;

            for (var index = 0; index < rootObjects.Count; index++)
            {
                var root = rootObjects[index];
                targetTransforms.Add(root.transform);
                GetObjectsInTransformHierarchy(root.transform, ref targetTransforms, numberOfObjects + 1);
            }
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

            return childCount;
        }

        /// <summary>
        /// Extracts geometry from Unity Mesh to geometry object
        /// </summary>
        /// <param name="seenObjects"></param>
        /// <param name="geoInfos"></param>
        /// <param name="targetedGeometries"></param>
        private void ExtractGeometry(HashSet<Transform> seenObjects, List<GeometryDataModels.GeoInfo> geoInfos,
            List<VisionTarget> targetedGeometries, bool collidersTargeted)
        {
            foreach (var seenObject in seenObjects)
            {
                var renderer = seenObject.GetComponent<Renderer>();
                if (renderer)
                {
                    var geoInfo = CreateGeoInfoObject(seenObject, renderer);
                    geoInfo = GetGeoInfoGeometryData(targetedGeometries, geoInfo, seenObject);

                    geoInfos.Add(geoInfo);
                }
            }

            GeometryDataModels.GeoInfo GetGeoInfoGeometryData(List<VisionTarget> targetedGeometries2,
                GeometryDataModels.GeoInfo geoInfo, Transform seenObject)
            {
                if (GeometryIsTargeted(targetedGeometries2))
                {
                    if (!collidersTargeted)
                    {
                        geoInfo.edges = MeshUtilities.GetEdgesFromMesh(geoInfo.renderer, geoInfo.mesh);
                    }
                    else
                    {
                        geoInfo.colliderMesh = seenObject.GetComponent<MeshCollider>().sharedMesh;
                        geoInfo.edges = MeshUtilities.GetEdgesFromMesh(geoInfo.renderer, geoInfo.mesh);
                    }
                }
                else
                {
                    geoInfo.edges = new GeometryDataModels.Edge[0];
                }

                return geoInfo;
            }
        }


        private static GeometryDataModels.GeoInfo CreateGeoInfoObject(Transform seenObject, Renderer renderer)
        {
            GeometryDataModels.GeoInfo geoInfo = new GeometryDataModels.GeoInfo();
            geoInfo.gameObject = seenObject.gameObject;
            geoInfo.transform = seenObject;
            geoInfo.edges = new GeometryDataModels.Edge[0];
            geoInfo.renderer = renderer;
            geoInfo.mesh = seenObject.GetComponent<MeshFilter>().mesh;
            return geoInfo;
        }

        /// <summary>
        /// Check if user has selected mesh geometry as target for the operation
        /// </summary>
        /// <param name="targetedGeometries"></param>
        /// <returns></returns>
        private bool GeometryIsTargeted(List<VisionTarget> targetedGeometries)
        {
            bool found = false;
            foreach (var visionTarget in targetedGeometries)
            {
                if (visionTarget.GeometryType == GeometryType.Lines ||
                    visionTarget.GeometryType == GeometryType.Vertices)
                {
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// gets all the gameobjects from scene
        /// </summary>
        /// <param name="rootObjects"></param>
        /// <param name="allObjects"></param>
        private void UpdateSceneObjects(List<GameObject> rootObjects, HashSet<Transform> allObjects, string tagName)
        {
            if (tagName.Length > 0)
            {
                GetTransforms(GameObject.FindGameObjectsWithTag(tagName).ToList(), ref allObjects);
            }
            else
            {
                GetTransformsFromRootObjects(rootObjects, ref allObjects);
            }
        }
    }
}