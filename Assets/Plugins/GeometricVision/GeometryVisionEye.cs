using System;
using System.Collections.Generic;
using GeometricVision;
using GeometricVision.Jobs;
using GeometricVision.Utilities;
using Plugins.GeometricVision;
using UniRx;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Serialization;
using Plane = UnityEngine.Plane;

/// <summary>
/// Class that is responsible for seeing objects and geometry.
/// It checks, if object is inside visibility area and filters out unwanted objects and geometry.
/// 
/// </summary>
public class GeometryVisionEye : MonoBehaviour
{
    [SerializeField] private bool debugMode;
    [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
    [SerializeField] private bool showSeenEdges = true;
    
    [SerializeField] private float fieldOfView = 25f;
    [SerializeField] private int lastCount = 0;
    [SerializeField] private List<GeometryDataModels.GeoInfo> seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
    [SerializeField] private IGeoBrain controllerBrain;
    private new Camera camera;
    public Plane[] planes = new Plane[6];
    [SerializeField] public HashSet<Transform> seenObjects;
    private EyeDebugger _debugger;
    private bool _addedByFactory;
    [SerializeField] private VisionTarget[] targetedGeometries;//TODO: Make it reactive and dispose subscribers on array resize in case they are not cleaned up by the gc
    public GeometryVisionHead Head { get; set; }
    
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
        if (targetedGeometries == null)
        {
            targetedGeometries = new VisionTarget[1];
            IGeoTargeting targeting = new GeometryObjectTargeting();
            targetedGeometries[0] = new VisionTarget(GeometryType.Objects_,0, targeting);
        }
        if (gameObject.GetComponent<Camera>() == null)
        {
            gameObject.AddComponent<Camera>();
            Camera1 = gameObject.GetComponent<Camera>();
        }
        seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
        ControllerBrain = GeometryVisionUtilities.getControllerFromGeometryManager(FindObjectOfType<GeometryVisionHead>(), this);

        _debugger = new EyeDebugger();
        seenObjects = new HashSet<Transform>();
        Camera1.enabled = false;
        
        if (debugMode)
        {
            _debugger.Planes = RegenerateVisionArea(fieldOfView, planes);
        }

        HandleTargeting();
    }

    void HandleTargeting()
    {
        foreach (var geometryType in TargetedGeometries)
        {
            UnityEngine.Debug.Log(geometryType.Target);
            if (geometryType.Target.Value == true)
            {
                var geoTargeting = gameObject.GetComponent<GeometryTargeting>();
                if ( gameObject.GetComponent<GeometryTargeting>() == null)
                {
                    gameObject.AddComponent<GeometryTargeting>();
                    geoTargeting = gameObject.GetComponent<GeometryTargeting>();

                }

                OnTargetingEnabled(geometryType, geoTargeting);
            }
        }
    }
    /// <summary>
    /// Add targeting implementation based on, if it is enabled on the inspector.
    /// Subscribes the targeting toggle button to functionality than handles creation of targeting implementation for the
    /// targeted geometry type
    /// </summary>
    /// <param name="geometryType"></param>
    /// <param name="geoTargeting"></param>
    private void OnTargetingEnabled(VisionTarget geometryType, GeometryTargeting geoTargeting)
    {
        if (!geometryType.Subscribed)
        {
            geometryType.Target.Subscribe(targeting =>
            {
                if (targeting)
                {
                    geoTargeting.AddTarget(geometryType);
                }
                else
                {
                    geoTargeting.RemoveTarget(geometryType);
                }
            });
            geometryType.Subscribed = true;
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        planes = RegenerateVisionArea(fieldOfView, planes);
        UpdateVisibility(seenObjects, seenGeoInfos);
        Debug();
    }

    /// <summary>
    /// Updates visibility of the objects in the eye and brain/manager
    /// </summary>
    /// <param name="seenObjects"></param>
    /// <param name="seenGeoInfos"></param>
    private void UpdateVisibility(HashSet<Transform> seenObjects, List<GeometryDataModels.GeoInfo> seenGeoInfos)
    {
        this.seenObjects = UpdateObjectVisibility(ControllerBrain.getAllObjects(), seenObjects);
        SeenGeoInfos = UpdateGeometryVisibility(planes, ControllerBrain.GeoInfos(), seenGeoInfos);
    }

    /// <summary>
    /// Update gameobject visibility. Object that do not have geometry in it
    /// </summary>
    private HashSet<Transform> UpdateObjectVisibility(List<Transform> listToCheck, HashSet<Transform> seenTransforms)
    {
        seenTransforms = new HashSet<Transform>();

        seenTransforms = GetObjectsInsideFrustum(seenTransforms, listToCheck);


        return seenTransforms;
    }

    /// <summary>
    /// Hides Edges, vertices, geometryObject outside th frustum
    /// </summary>
    /// <param name="planes"></param>
    /// <param name="allGeoInfos"></param>
    private List<GeometryDataModels.GeoInfo> UpdateGeometryVisibility(Plane[] planes,
        List<GeometryDataModels.GeoInfo> allGeoInfos, List<GeometryDataModels.GeoInfo> seenGeometry)
    {
        int geoCount = allGeoInfos.Count;
        seenGeometry = new List<GeometryDataModels.GeoInfo>();

        UpdateSeenGeometryObjects(allGeoInfos, seenGeometry, geoCount);

        foreach (var geometryType in TargetedGeometries)
        {
            if (geometryType.GeometryType == GeometryType.Edges)
            {
                MeshUtilities.UpdateEdgesVisibilityParallel(planes, seenGeometry);
            }
        }

        return seenGeometry;
    }

    /// <summary>
    /// Updates object collection containing geometry and data related to seen object. Usage is to internally update seen geometry objects by checking objects renderer bounds
    /// against eyes/cameras frustum
    /// </summary>
    /// <param name="allGeoInfos"></param>
    /// <param name="seenGeometry"></param>
    /// <param name="geoCount"></param>
    private void UpdateSeenGeometryObjects(List<GeometryDataModels.GeoInfo> allGeoInfos,
        List<GeometryDataModels.GeoInfo> seenGeometry, int geoCount)
    {
        if (geometryIsTargeted())
        {
            for (var i = 0; i < geoCount; i++)
            {
                var geInfo = allGeoInfos[i];

                if (GeometryUtility.TestPlanesAABB(planes, allGeoInfos[i].renderer.bounds) &&
                    hideEdgesOutsideFieldOfView)
                {
                    seenGeometry.Add(geInfo);
                }
            }
        }
    }

    /// <summary>
    /// Check if user has selected mesh geometry as target for the operation
    /// </summary>
    /// <returns></returns>
    private bool geometryIsTargeted()
    {
        bool found = false;
        foreach (var visionTarget in TargetedGeometries)
        {
            if (visionTarget.GeometryType == GeometryType.Edges || visionTarget.GeometryType == GeometryType.Vertices)
            {
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// When the camera is moved, rotated or both the frustum planes that
    /// hold the system together needs to be refreshes/regenerated
    /// </summary>
    /// <param name="fieldOfView"></param>
    /// <returns>Plane[]</returns>
    /// <remarks>Faster way to get the current situation for planes might be to store planes into an object and move them with the eye</remarks>
    private Plane[] RegenerateVisionArea(float fieldOfView, Plane[] planes)
    {
        Camera1.enabled = true;
        Camera1.fieldOfView = fieldOfView;
        planes = GeometryUtility.CalculateFrustumPlanes(Camera1);
        Camera1.enabled = false;
        return planes;
    }

    /// <summary>
    /// When the camera is moved, rotated or both the frustum planes that
    /// hold the system together needs to be refreshes/regenerated
    /// </summary>
    /// <param name="fieldOfView"></param>
    /// <returns>void</returns>
    /// <remarks>Faster way to get the current situation for planes might be to store planes into an object and move them with the eye</remarks>
    public void RegenerateVisionArea(float fieldOfView)
    {
        Camera1.enabled = true;
        Camera1.fieldOfView = fieldOfView;
        planes = GeometryUtility.CalculateFrustumPlanes(Camera1);
        Camera1.enabled = false;
    }

    private HashSet<Transform> GetObjectsInsideFrustum(HashSet<Transform> seenTransforms, List<Transform> allTransforms)
    {
        foreach (var transform in allTransforms)
        {
            if (MeshUtilities.IsInsideFrustum(transform.position, planes))
            {
                seenTransforms.Add(transform);
                lastCount = seenTransforms.Count;
            }
        }

        return seenTransforms;
    }


    private void RemoveAt(ref Renderer[] renderer, int indexToRemove)
    {
        for (int i = indexToRemove; i < renderer.Length - 1; i++)
        {
            renderer[i] = renderer[i + 1];
        }

        Destroy(gameObject.GetComponent<Camera>());

        Array.Resize(ref renderer, renderer.Length - 1);
    }

    private void Debug()
    {
        if (debugMode)
        {
            _debugger.Debug(Camera1, SeenGeoInfos, true);
        }
    }
    
    public VisionTarget[] TargetedGeometries
    {
        get { return targetedGeometries; }
        set { targetedGeometries = value; }
    }

    public List<GeometryDataModels.GeoInfo> SeenGeoInfos
    {
        get { return seenGeoInfos; }
        set { seenGeoInfos = value; }
    }
    
    public Plane[] Planes
    {
        get { return planes; }
        set { planes = value; }
    }

    public Camera Camera1
    {
        get { return camera; }
        set { camera = value; }
    }

    public IGeoBrain ControllerBrain
    {
        get { return controllerBrain; }
        set { controllerBrain = value; }
    }
}