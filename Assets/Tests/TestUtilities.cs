using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeometricVision;
using Plugins.GeometricVision;
using Tests;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TestUtilities
{
    public static Scene newScene(NewSceneSetup setup)
    {
        Scene scene = EditorSceneManager.NewScene(setup, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        return scene;
    }

    public static int GetObjectCountFromScene(Vector3 coordinates)
    {
        var rootObjects = new List<GameObject>();
        SceneManager.GetActiveScene().GetRootGameObjects(rootObjects);
        int expectedObjectCount = 0;
        
        foreach (var root in rootObjects)
        {
            if (root.transform.position.x == coordinates.x && root.transform.position.y == coordinates.y &&
                root.transform.position.z == coordinates.z)
            {
                expectedObjectCount += root.GetComponentsInChildren<Transform>().Length;
            }
        }

        return expectedObjectCount;
    }

    public static int GetObjectCountFromScene()
    {
        var rootObjects = new List<GameObject>();
        SceneManager.GetActiveScene().GetRootGameObjects(rootObjects);
        
        int expectedObjectCount = 0;
        foreach (var root in rootObjects)
        {
            expectedObjectCount += root.GetComponentsInChildren<Transform>().Length;
        }

        return expectedObjectCount;
    }
    
    /// <summary>
    /// Gets scene paths for GameObject tests.
    /// Usage: Used as parameter in tests. See written tests and ValueSource from docs
    /// </summary>
    /// <returns></returns>
    public static IEnumerable GetSimpleTestScenePathsForGameObjects()
    {
        var testSceneFolderInAssetsFolder = TestSettings.GameObjectsSimpleTestsPath;
        var sceneFolderPath = Application.dataPath + "/" + testSceneFolderInAssetsFolder;
        List<string> scenePaths = GetSceneFilePaths(sceneFolderPath, testSceneFolderInAssetsFolder).ToList();

        return scenePaths;
    }
        
    /// <summary>
    /// Gets scene paths for GameObject tests.
    /// Usage: Used as parameter in tests. See written tests and ValueSource from docs
    /// </summary>
    /// <returns></returns>
    public static IEnumerable GetEmptyScenePathsForGameObjects()
    {
        var testSceneFolderInAssetsFolder = TestSettings.EmptyScenePath;
        var sceneFolderPath = Application.dataPath + "/" + testSceneFolderInAssetsFolder;
        List<string> scenePaths = GetSceneFilePaths(sceneFolderPath, testSceneFolderInAssetsFolder).ToList();

        return scenePaths;
    }
    /// <summary>
    /// Gets scene paths for GameObject tests.
    /// Usage: Used as parameter in tests. See written tests and ValueSource from docs
    /// </summary>
    /// <returns></returns>
    public static IEnumerable GetTargetingTestScenePathsForGameObjects()
    {
        var testSceneFolderInAssetsFolder = TestSettings.GameObjectsTargetingTests;
        var sceneFolderPath = Application.dataPath + "/" + testSceneFolderInAssetsFolder;
        List<string> scenePaths = GetSceneFilePaths(sceneFolderPath, testSceneFolderInAssetsFolder).ToList();

        return scenePaths;
    }
    
    /// <summary>
    /// Gets scene paths for entity tests.
    /// Usage: Used as parameter in tests. See written tests and ValueSource from docs
    /// </summary>
    /// <returns></returns>
    public static IEnumerable GetSimpleTestScenePathsForEntities()
    {
        var testSceneFolderInAssetsFolder = TestSettings.EntitiesSimpleTestsPath;
        var sceneFolderPath = Application.dataPath + "/" + testSceneFolderInAssetsFolder;
        List<string> scenePaths = GetSceneFilePaths(sceneFolderPath, testSceneFolderInAssetsFolder).ToList();

        return scenePaths;
    }
        
    /// <summary>
    /// Gets scene paths for entity tests.
    /// Usage: Used as parameter in tests. See written tests and ValueSource from docs
    /// </summary>
    /// <returns></returns>
    public static IEnumerable GetTargetingTestScenePathsForEntities()
    {
        var testSceneFolderInAssetsFolder = TestSettings.EntitiesTargetingTests;
        var sceneFolderPath = Application.dataPath + "/" + testSceneFolderInAssetsFolder;
        List<string> scenePaths = GetSceneFilePaths(sceneFolderPath, testSceneFolderInAssetsFolder).ToList();

        return scenePaths;
    }
    
    public static string[] GetSceneFilePaths(string sceneFolderPath, string testSceneFolder)
    {
        DirectoryInfo d = new DirectoryInfo(@sceneFolderPath);
        FileInfo[] scenePaths = d.GetFiles("*.unity");
        string[] scenePaths2 = new string[scenePaths.Length];
        for (var index = 0; index < scenePaths.Length; index++)
        {
            var sceneName = scenePaths[index].Name;
            scenePaths2[index] = "Assets/"+testSceneFolder + sceneName;
        }

        return scenePaths2;
    }
    
    internal static List<string> CreateScenePathFromRelativeAddress(string relativeFolder)
    {
        var sceneFolderPath = Application.dataPath + "/" + relativeFolder;
        List<string> scenePaths = GetSceneFilePaths(sceneFolderPath, relativeFolder).ToList();
        return scenePaths;
    }

    public static GameObject SetupGeoVision2(Vector3 position, GeometryVisionFactory factory, bool edgesTargeted)
    {
        var geoTypesToTarget = new List<GeometryType>();
        if (edgesTargeted)
        {
            geoTypesToTarget.Add(GeometryType.Lines);
        }

        GameObject geoVision = factory.CreateGeometryVision(position, Quaternion.identity, 25, geoTypesToTarget,  true);
        return geoVision;
    }

    public static GameObject SetupGeoVision(Vector3 position, GeometryVisionFactory factory)
    {
        var geoTypesToTarget = new List<GeometryType>();
        geoTypesToTarget.Add(GeometryType.Objects);
        GameObject geoVision = factory.CreateGeometryVision(position, Quaternion.identity, 25, geoTypesToTarget,  true);
        return geoVision;
    }

    public static IEnumerable GetScenesFromBuildSettings()
    {
        string testSceneIdentifier = "_frustum_test";
        List<string> scenePaths = new List<string>();
        var amountOfScenesInBuildSettings = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < amountOfScenesInBuildSettings; i++)
        {
            var pathToScene = SceneUtility.GetScenePathByBuildIndex(i);
            if (pathToScene.Contains(testSceneIdentifier))
            {
                scenePaths.Add(pathToScene);
            }

            Debug.Log("Scene found named: " + pathToScene);
        }

        scenePaths.Sort();
        return scenePaths;
    }

    public static EditorBuildSettingsScene[] SetupBuildSettings(string scenePath)
    {
        Debug.Log("SetupBuildSettings: " + scenePath);

        EditorBuildSettingsScene[] originalScenes = AddSceneToBuildSettings(scenePath);

        return originalScenes;
    }
 
    public static void SetupScene(string scenePath)
    {
        Time.timeScale = 100f;
        Debug.Log("Loading: " + scenePath);
        //Load first scene
        SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
        
    }

    private static EditorBuildSettingsScene[] AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> testScenes = new List<EditorBuildSettingsScene>();
        foreach (var editorBuildSettingsScene in originalScenes)
        {
            testScenes.Add(editorBuildSettingsScene);
        }

        Debug.Log("scene path to add to build settings: " + scenePath);
        var scene = new EditorBuildSettingsScene(scenePath, true);

        testScenes.Add(scene);
        EditorBuildSettings.scenes = testScenes.ToArray();
        
        foreach (var editorBuildSettingsScene in EditorBuildSettings.scenes)
        {
            Debug.Log(editorBuildSettingsScene.path);
        }
        Debug.Log("----EditorBuildSettings.scenes " +EditorBuildSettings.scenes[0].path); 
        Debug.Log("----amount of scenes loaded: EditorBuildSettings.scenes.Length" + EditorBuildSettings.scenes.Length);
        Debug.Log("----amount of scenes loaded SceneManager.sceneCount: " + EditorSceneManager.sceneCount);
        return originalScenes;
    }

    public static  EditorBuildSettingsScene[] SetupBuildSettings(List<string> getScenesFromPathList)
    {
        EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> testScenes = originalScenes.ToList();
        
        
        foreach (var scenePath in getScenesFromPathList)
        {
            Debug.Log("scene path to add to build settings: " + scenePath);
            var scene = new EditorBuildSettingsScene(scenePath, true);
            testScenes.Add(scene);
        }
        


        if (originalScenes.Length > 0)
        {
            Debug.Log("----original: " + originalScenes[0].path);
        }

        Debug.Log("----" + testScenes[0].path); 
        

        EditorBuildSettings.scenes = testScenes.ToArray();
        if ( EditorBuildSettings.scenes.Length !=0)
        {
            foreach (var editorBuildSettingsScene in EditorBuildSettings.scenes)
            {
                Debug.Log(editorBuildSettingsScene.path);
            }

        }        

        Debug.Log("----EditorBuildSettings.scenes " +EditorBuildSettings.scenes[0].path); 
        Debug.Log("----amount of scenes loaded: EditorBuildSettings.scenes.Length: " + EditorBuildSettings.scenes.Length);
        Debug.Log("----amount of scenes loaded SceneManager.sceneCount: " + EditorSceneManager.sceneCount);
        var returnValues =originalScenes;

        return returnValues;
    }
    
    public static void PostCleanUpBuildSettings(EditorBuildSettingsScene[] originalScenes)
    {
        EditorSceneManager.LoadScene(0, LoadSceneMode.Single);
        EditorBuildSettings.scenes = originalScenes;
        
        foreach (var editorBuildSettingsScene in EditorBuildSettings.scenes)
        {
            Debug.Log(editorBuildSettingsScene.path);
        }
        Debug.Log("----amount of scenes loaded: EditorBuildSettings.scenes.Length" + EditorBuildSettings.scenes.Length);
        Debug.Log("----amount of scenes loaded SceneManager.sceneCount: " + EditorSceneManager.sceneCount);
    }

    public static bool CheckThatImplementationIsOnTheList<T>(HashSet<T> listToCheck, Type type)
    {
        bool found = false;
            
        foreach (var targetingProgram in listToCheck)
        {
            if (targetingProgram.GetType() == type)
            {
                found = true;
            }
        }

        return found;
    }
}