using System;
using System.Collections.Generic;
using System.Linq;
using Plugins.GeometricVision.Interfaces;
using Plugins.GeometricVision.Utilities;
using Unity.Collections;
using UnityEngine;

namespace Plugins.GeometricVision.ImplementationsGameObjects
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

        private void Awake()
        {
            this.hideFlags = HideFlags.HideInInspector;
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

        /// <summary>
        /// Updates visibility of the objects in the eye and processor/manager
        /// </summary>
        public void UpdateVisibility(bool useBounds)
        {
            SeenGeoInfos = UpdateGeometryInfoVisibilities(GeoVision.Planes, Runner.GeoMemory.GeoInfos, useBounds);
        }

        public NativeArray<GeometryDataModels.Edge> GetSeenEdges()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates Data structures made for Game Objects visibility.
        /// </summary>
        /// <param name="planes"></param>
        /// <param name="allGeoInfos"></param>
        /// <param name="useBounds"></param>
        private List<GeometryDataModels.GeoInfo> UpdateGeometryInfoVisibilities(Plane[] planes,
            List<GeometryDataModels.GeoInfo> allGeoInfos, bool useBounds)
        {
            int geoCount = allGeoInfos.Count;
            int index2 = 0;
            var newGeoInfos = new List<GeometryDataModels.GeoInfo>(allGeoInfos.Count);
            newGeoInfos.AddRange(allGeoInfos);
            this.seenTransforms = new List<Transform>();
            UpdateSeenGeometryObjects();

            // Updates object collection containing geometry and data related to seen object. Usage is to internally update seen geometry objects by checking objects renderer bounds
            // against eyes/cameras frustum
            void UpdateSeenGeometryObjects()
            {
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
                        AddVisibleRenderer(geoInfo, ref index2);
                    }
                    else if (MeshUtilities.IsInsideFrustum(geoInfoTransform.position, planes))
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
                if (!geInfo.renderer)
                {
                    newGeoInfos.Remove(geInfo);
                    return;
                }

                if (GeometryUtility.TestPlanesAABB(GeoVision.Planes, geInfo.renderer.bounds))
                {
                   
                    if (GeometryVisionUtilities.TransformIsEffect(geInfo.transform.name))
                    {
                        return;
                    }
                    this.seenTransforms.Add(geInfo.transform);
                    this.newSeenGeometriesList[index] =geInfo;
                    index += 1;
                }
            }

            void AddVisibleObject(GeometryDataModels.GeoInfo geInfo, ref int index)
            {
                if (GeometryVisionUtilities.TransformIsEffect(geInfo.transform.name))
                {
                    return;
                }
                this.seenTransforms.Add(geInfo.transform);
                this.newSeenGeometriesList[index] =geInfo;
                index += 1;
            }
        }
 
        public List<GeometryDataModels.GeoInfo> SeenGeoInfos
        {
            get { return seenGeoInfos; }
            set { seenGeoInfos = value; }
        }
    }
}