using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GeometricVision;
using Plugins.GeometricVision;
using UnityEditor;
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
    
    public static IEnumerable GetScenesFromPath()
    {
        var sceneFolderPath =Application.dataPath+ "/Tests/TestScenes/";
        List<string> scenePaths= GetSceneFilePaths(sceneFolderPath).ToList();

        return scenePaths;
    }

    public static GameObject SetupGeoVision(Vector3 position,
        Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes, bool edgesTargeted)
    {
        var geoTypesToTarget = new List<GeometryType>();
        if (edgesTargeted)
        {
            geoTypesToTarget.Add(GeometryType.Lines);
        }

        GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(position,
            Quaternion.identity, 25,
            geoTypesToTarget, 0, true);
        return geoVision;
    }
    public static string[] GetSceneFilePaths( string sceneFolderPath)
    {
        DirectoryInfo d = new DirectoryInfo(@sceneFolderPath);
        FileInfo[] scenePaths = d.GetFiles( "*.unity");
        string[] scenePaths2 = new string[scenePaths.Length];
        for (var index = 0; index < scenePaths.Length; index++)
        {
            var scenePath = scenePaths[index].Name;
            scenePaths2[index] = "Assets/Tests/TestScenes/"+scenePath;
        }

        return scenePaths2;
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

    public static Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> SetupScene(string scenePath)
    {
        Time.timeScale = 100f;
        Debug.Log("Loading: " + scenePath);

        EditorBuildSettingsScene[] originalScenes = AddSceneToBuildSettings(scenePath);

        EditorSceneManager.LoadScene(0, LoadSceneMode.Single);

        var returnValues = new Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]>( new GeometryVisionFactory(), originalScenes);

        return returnValues;
    }

    private static EditorBuildSettingsScene[] AddSceneToBuildSettings(string scenePath)
    {
        var amountOfScenesInBuildSettings = SceneManager.sceneCountInBuildSettings;
        EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] testScenes = new EditorBuildSettingsScene[1];


        Debug.Log("scene path to add to build settings: " + scenePath);
        var scene = new EditorBuildSettingsScene(scenePath, true);
        testScenes[0] = scene;
        //Debug.Log("----original" + originalScenes[0].path);
        Debug.Log("----" + testScenes[0].path);
        EditorBuildSettings.scenes = testScenes;
        Debug.Log("----amount of scenes loaded" + EditorBuildSettings.scenes.Length );
        return originalScenes;
    }
}