using System;
using System.Collections.Generic;
using GeometricVision;
using GeometricVision.Jobs;
using GeometricVision.Utilities;
using Plugins.GeometricVision;
using UniRx;
using Unity.Jobs;
using UnityEngine;
using Plane = UnityEngine.Plane;

/// <summary>
/// Class that is responsible for seeing objects and geometry.
/// It checks, if object is inside visibility area and filters out unwanted objects and geometry.
/// 
/// </summary>
public class GeometryVisionEye : MonoBehaviour
{
    [SerializeField] private bool debugMode;

    
    public VisionTarget[] GeometryTypes
    {
        get { return geometryTypes; }
        set { geometryTypes = value; }
    }

    [SerializeField] private bool hideEdgesOutsideFieldOfView = true;
    [SerializeField] private bool showSeenEdges = true;
    
    [SerializeField] private float fieldOfView = 25f;
    [SerializeField] private int lastCount = 0;
    [SerializeField] private List<GeometryDataModels.GeoInfo> _seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
    [SerializeField] private IGeoBrain _controllerBrain;
    private Camera camera;
    public Plane[] _planes = new Plane[6];
    [SerializeField] public HashSet<Transform> SeenObjects;
    private EyeDebugger _debugger;
    private bool _addedByFactory;
    [SerializeField] private VisionTarget[] geometryTypes;

    public List<GeometryDataModels.GeoInfo> SeenGeoInfos
    {
        get { return _seenGeoInfos; }
        set { _seenGeoInfos = value; }
    }

    public struct filterPlane
    {
        public Vector3 positiom;
        public Quaternion rotation;
        public Vector3 direction;
        public Transform rootObject;
    }


    public Plane[] Planes
    {
        get { return _planes; }
        set { _planes = value; }
    }

    public Camera Camera1
    {
        get { return camera; }
        set { camera = value; }
    }

    public IGeoBrain ControllerBrain
    {
        get { return _controllerBrain; }
        set { _controllerBrain = value; }
    }

    public GeometryVisionHead Head { get; set; }


    private void Awake()
    {
        if (gameObject.GetComponent<Camera>() != null)
        {
            Camera1 = gameObject.GetComponent<Camera>();
        }

        _seenGeoInfos = new List<GeometryDataModels.GeoInfo>();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        if (gameObject.GetComponent<Camera>() == null)
        {
            gameObject.AddComponent<Camera>();
            Camera1 = gameObject.GetComponent<Camera>();
        }


        ControllerBrain = GeometryVisionUtilities.getControllerFromGeometryManager( FindObjectOfType<GeometryVisionHead>(), this);

        _debugger = new EyeDebugger();
        SeenObjects = new HashSet<Transform>();
        Camera1.enabled = false;
        _debugger.Planes = RegenerateVisionArea(fieldOfView, _planes);
        handLeTargeting();

    }

    void handLeTargeting()
    {
        foreach (var geometryType in   GeometryTypes)
        {
            UnityEngine.Debug.Log(geometryType);
        }
            
    }

    // Update is called once per frame
    void Update()
    {
        _planes = RegenerateVisionArea(fieldOfView, _planes);
        UpdateVisibility(SeenObjects, _seenGeoInfos);
        Debug();
    }

    /// <summary>
    /// Updates visibility of the objects in the eye and brain/manager
    /// </summary>
    /// <param name="seenObjects"></param>
    /// <param name="seenGeoInfos"></param>
    private void UpdateVisibility(HashSet<Transform> seenObjects, List<GeometryDataModels.GeoInfo> seenGeoInfos)
    {
        SeenObjects = UpdateObjectVisibility(ControllerBrain.getAllObjects(), seenObjects);
        SeenGeoInfos = UpdateGeometryVisibility(_planes, ControllerBrain.GeoInfos(), seenGeoInfos);
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

        foreach (var geometryType in GeometryTypes)
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

                if (GeometryUtility.TestPlanesAABB(_planes, allGeoInfos[i].renderer.bounds) &&
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
        foreach (var visionTarget in GeometryTypes)
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
        _planes = GeometryUtility.CalculateFrustumPlanes(Camera1);
        Camera1.enabled = false;
    }

    private HashSet<Transform> GetObjectsInsideFrustum(HashSet<Transform> seenTransforms, List<Transform> allTransforms)
    {
        foreach (var transform in allTransforms)
        {
            if (MeshUtilities.IsInsideFrustum(transform.position, _planes))
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
}