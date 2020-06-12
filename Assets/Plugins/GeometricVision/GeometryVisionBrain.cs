using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GeometricVision;
using GeometricVision.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;
[DisallowMultipleComponent]
public class GeometryVisionBrain : MonoBehaviour, IGeoBrain
{
    [SerializeField]
    private int lastCount = 0;

    [SerializeField]
    private List<GeometryDataModels.GeoInfo> _geoInfos = new List<GeometryDataModels.GeoInfo>();
    public HashSet<Transform> AllObjects;
    public List<GameObject> RootObjects;


    List<GeometryDataModels.GeoInfo> IGeoBrain.GeoInfos()
    {
        return _geoInfos;
    }

    public int CountSceneObjects()
    {        
        SceneManager.GetActiveScene().GetRootGameObjects(RootObjects);
        return CountObjectsInHierarchy(RootObjects);
    }

    public HashSet<Transform> GetTransforms(List<GameObject> objs)
    {
        var result = new HashSet<Transform>();
        GetTransforms(objs, ref result);
        return result;
    }

    public List<Transform> getAllObjects()
    {
        return AllObjects.ToList();
    }

    public List<GeometryDataModels.GeoInfo> GeoInfos
    {
        get { return _geoInfos; }
        set { _geoInfos = value; }
    }

    // Start is called before the first frame update
    void Awake()
    {
        GeoInfos = new List<GeometryDataModels.GeoInfo>();
        AllObjects = new HashSet<Transform>();
        RootObjects = new List<GameObject>();

    }

    // Update is called once per frame
    void Update()
    {
        CheckSceneChanges();
    }
    
    /// <summary>
    /// Used to check, if things inside scene has changed. Like if new object has been removed or moved.
    /// </summary>
    private void CheckSceneChanges()
    {
        SceneManager.GetActiveScene().GetRootGameObjects(RootObjects);
        
        var currentObjectCount = CountObjectsInHierarchy(RootObjects);
        if (currentObjectCount != lastCount)
        {
            lastCount = currentObjectCount;
            UpdateSceneObjects(RootObjects, AllObjects);
            ExtractGeometry(AllObjects, GeoInfos);
        }
    }
    /// <summary>
    /// Goes through all the root objects and counts their children
    /// </summary>
    /// <param name="rootGameObjects"></param>
    /// <returns></returns>
    public int CountObjectsInHierarchy(List<GameObject> rootGameObjects)
    {
        int numberOfObjects = 0;
        foreach (var root in rootGameObjects)
        {
            numberOfObjects = CountObjectsInTransformHierarchy(root.transform, numberOfObjects+1);
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
            numberOfObjects= CountObjectsInTransformHierarchy(transform, numberOfObjects+ 1);
        }
        return numberOfObjects;
    }
    
    /// <summary>
    /// Gets all the trasforms from list of objects
    /// </summary>
    /// <param name="rootObjects"></param>
    /// <param name="targetTransforms"></param>
    /// <returns></returns>
    public void GetTransforms(List<GameObject> rootObjects, ref HashSet<Transform> targetTransforms)
    {
        int numberOfObjects = 0;

        for (var index = 0; index < rootObjects.Count; index++)
        {
            var root = rootObjects[index];
            targetTransforms.Add(root.transform);
            getObjectsInTransformHierarchy(root.transform, ref targetTransforms, numberOfObjects + 1);
        }
    }
    private static int getObjectsInTransformHierarchy(Transform root, ref HashSet<Transform> targetList, int numberOfObjects)
    {
        int childCount = root.childCount;
        for (var index = 0; index < childCount; index++)
        {
            targetList.Add(root.GetChild(index));
            getObjectsInTransformHierarchy(root.GetChild(index), ref targetList, numberOfObjects+ 1);
        }
        return childCount;
    }
    /// <summary>
    /// Extracts geometry from Unity Mesh to geometry object
    /// </summary>
    /// <param name="seenObjects"></param>
    /// <param name="geoInfos"></param>
    private void ExtractGeometry(HashSet<Transform> seenObjects, List<GeometryDataModels.GeoInfo> geoInfos)
    {
        foreach (var seenObject in seenObjects)
        {
            var renderer = seenObject.GetComponent<Renderer>();
            if (renderer)
            {
                GeometryDataModels.GeoInfo geoInfo = new GeometryDataModels.GeoInfo();
                geoInfo.gameObject = seenObject.gameObject;
                geoInfo.transform = seenObject;
                geoInfo.edges = new GeometryDataModels.Edge[0];
                geoInfo.renderer = renderer;
                geoInfo.mesh = seenObject.GetComponent<MeshFilter>().mesh;
                geoInfo.edges = MeshUtilities.GetEdgesFromMesh(geoInfo.renderer, geoInfo.mesh);
                geoInfos.Add(geoInfo);
            }
        }
    }
    /// <summary>
    /// gets all the gameobjects from scene
    /// </summary>
    /// <param name="rootObjects"></param>
    /// <param name="allObjects"></param>
    private void UpdateSceneObjects(List<GameObject> rootObjects, HashSet<Transform> allObjects)
    {
        GetTransforms(rootObjects, ref allObjects);

        int j = 0;
        foreach (var go in AllObjects.ToList())//Copy to list so we are not modifiyng the same collection as we are iterating
        {
            if (go.name == "geoVision")
            {
                AllObjects.Remove(go);
            }
        }
    }

}
