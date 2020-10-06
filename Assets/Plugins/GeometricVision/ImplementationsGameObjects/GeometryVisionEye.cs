using System.Collections.Generic;
using System.Linq;
using GeometricVision;
using Plugins.GeometricVision.ImplementationsGameObjects;
using Plugins.GeometricVision.Utilities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace Plugins.GeometricVision.Interfaces.Implementations
{
    /// <summary>
    /// Class that is responsible for seeing objects and geometry.
    /// It checks, if object is inside visibility area and filters out unwanted objects and geometry.
    ///
    /// Usage: Add to objects you want it to. The component will handle the rest. Component has list of geometry types.
    /// These are used to see certain type of objects and clicking the targeting option from the inspector UI the user can
    /// add option to find the closest element of this type.
    /// </summary>
    [DisallowMultipleComponent]
    public class GeometryVisionEye : MonoBehaviour, IGeoEye
    {
        public string Id { get; set; }
        [SerializeField] private bool debugMode;
        [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
        [SerializeField] private int lastCount = 0;
        [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
        [SerializeField] public List<Transform> seenTransforms;
        [SerializeField, Tooltip(" Geometry is extracted from collider instead of renderers mesh")]
        private bool targetColliderMeshes;
        private GeometryDataModels.GeoInfo[] newSeenGeometriesList = new GeometryDataModels.GeoInfo[500000];
        public GeometryVision GeoVision { get; set; }

        public GeometryVisionRunner Runner { get; set; }

        void Reset()
        {
            Initialize();
        }

        // Start is called before the first frame update
        void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
            seenTransforms = new List<Transform>();
        }

        public NativeArray<GeometryDataModels.Edge> GetSeenEdges()
        {
            List<GeometryDataModels.Edge> seenEdges = new List<GeometryDataModels.Edge>();
            int visibleEdgeCount = 0;
            foreach (var geo in SeenGeoInfos)
            {
                foreach (var edge1 in geo.edges.Where(edge => edge.isVisible == true))
                {
                    seenEdges.Add(edge1);
                    visibleEdgeCount += 1;
                }
            }

            return new NativeArray<GeometryDataModels.Edge>(seenEdges.ToArray(), Allocator.Temp);
        }


        /// <summary>
        /// Updates visibility of the objects in the eye and processor/manager
        /// </summary>
        public void UpdateVisibility(bool useBounds)
        {
            seenTransforms =
                UpdateTransformVisibility(GeoVision.Runner.GetProcessor<GeometryVisionProcessor>().GetAllTransforms(),
                    seenTransforms);
            SeenGeoInfos = UpdateRenderedMeshVisibility(GeoVision.Planes, Runner.GeoMemory.GeoInfos, useBounds);
        }


        /// <summary>
        /// Update GameObject/transform visibility. Object that does not have Mesh or renderer in it
        /// </summary>
        private List<Transform> UpdateTransformVisibility(List<Transform> transformsToCheck,
            List<Transform> seenTransforms)
        {
            seenTransforms = new List<Transform>();

            seenTransforms = GetTransformsInsideFrustum(seenTransforms, transformsToCheck);


            return seenTransforms;
        }

        /// <summary>
        /// Checks all the object that contain render component if they are visible by testing their bounding box
        /// Hides Edges, vertices, geometryObject outside th frustum
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="allGeoInfos"></param>
        /// <param name="useBounds"></param>
        private List<GeometryDataModels.GeoInfo> UpdateRenderedMeshVisibility(Plane[] planes,
            List<GeometryDataModels.GeoInfo> allGeoInfos, bool useBounds)
        {
            int geoCount = allGeoInfos.Count;
            int index2 = 0;
            var newGeoInfos = new List<GeometryDataModels.GeoInfo>(allGeoInfos.Count);
            newGeoInfos.AddRange(allGeoInfos);
            UpdateSeenGeometryObjects();

            // Updates object collection containing geometry and data related to seen object. Usage is to internally update seen geometry objects by checking objects renderer bounds
            // against eyes/cameras frustum
            void UpdateSeenGeometryObjects()
            {
                var nameOfTransform = "";
                Transform geoInfoTransform = null;
                for (var i = 0; i < geoCount; i++)
                {
                    var geoInfo = allGeoInfos[i];
                    geoInfoTransform = geoInfo.transform;
                    if (!geoInfoTransform)
                    {
                        continue;
                    }

                    if (useBounds)
                    {
                        AddVisibleRenderer(geoInfo,ref index2);
                    }
                    else if (MeshUtilities.IsInsideFrustum(geoInfoTransform.position, GeoVision.Planes))
                    {
                        AddVisibleObject(geoInfo, ref index2);
                    }
                }

                allGeoInfos = newGeoInfos;
                newGeoInfos = null;
            }
            
            return newSeenGeometriesList.Take(index2).ToList();

            // Local functions
            void AddVisibleRenderer(GeometryDataModels.GeoInfo geInfo, ref int index)
            {
                string nameOfTransform;
                if (!geInfo.renderer)
                {
                    newGeoInfos.Remove(geInfo);
                    return;
                }

                if (GeometryUtility.TestPlanesAABB(GeoVision.Planes, geInfo.renderer.bounds))
                {
                    nameOfTransform = geInfo.transform.name;
                    if (GeometryVisionUtilities.TransformIsEffect(nameOfTransform))
                    {
                        return;
                    }

                    newSeenGeometriesList[index] =geInfo;
                    index += 1;
                }
            }

            void AddVisibleObject(GeometryDataModels.GeoInfo geInfo, ref int index)
            {
            
                string nameOfTransform = geInfo.transform.name;
                if (GeometryVisionUtilities.TransformIsEffect(nameOfTransform))
                {
                    return;
                }

                newSeenGeometriesList[index] =geInfo;
                index += 1;
            }
        }
        
        private List<Transform> GetTransformsInsideFrustum(List<Transform> seenTransforms, List<Transform> allTransforms)
        {
            List<Transform> allTempTransforms = new List<Transform>(allTransforms);
            string transformName;
            foreach (var transformInProcess in allTransforms)
            {
                if (transformInProcess)
                {
                    if (MeshUtilities.IsInsideFrustum(transformInProcess.position, GeoVision.Planes))
                    {
                        transformName = transformInProcess.name;
                        if (GeometryVisionUtilities.TransformIsEffect(transformName))
                        {
                            continue;
                        }

                        seenTransforms.Add(transformInProcess);
                        lastCount = seenTransforms.Count;
                    }
                }
                else
                {
                    allTempTransforms.Remove(transformInProcess);
                }
            }

            allTransforms = allTempTransforms;
            return seenTransforms;
        }
          
        
        public List<GeometryDataModels.GeoInfo> SeenGeoInfos
        {
            get { return seenGeoInfos; }
            set { seenGeoInfos = value; }
        }

        public bool DebugMode
        {
            get { return debugMode; }
            set { debugMode = value; }
        }


        public bool TargetColliderMeshes
        {
            get { return targetColliderMeshes; }
            set { targetColliderMeshes = value; }
        }
    }
}