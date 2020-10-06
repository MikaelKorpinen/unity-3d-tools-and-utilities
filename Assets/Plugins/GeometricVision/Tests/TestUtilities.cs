using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plugins.GeometricVision.Tests
{
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
        /// Gets scene paths for GameObject stress tests.
        /// Usage: Used as parameter in tests. See written tests and ValueSource from docs
        /// </summary>
        /// <returns></returns>
        public static IEnumerable GetStressTestsScenePathsForGameObjects()
        {
            var testSceneFolderInAssetsFolder = TestSettings.GameObjectsStressTestsPath;
            var sceneFolderPath = Application.dataPath + "/" + testSceneFolderInAssetsFolder;
            List<string> scenePaths = GetSceneFilePaths(sceneFolderPath, testSceneFolderInAssetsFolder).ToList();

            return scenePaths;
        }
                
        /// <summary>
        /// Gets scene paths for entities stress tests.
        /// Usage: Used as parameter in tests. See written tests and ValueSource from docs
        /// </summary>
        /// <returns></returns>
        public static IEnumerable GetStressTestsScenePathsForEntities()
        {
            var testSceneFolderInAssetsFolder = TestSettings.EntitiesStressTestsPath;
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
        
        public static GameObject SetupGeoVision(Vector3 position, GeometryVisionFactory factory)
        {
            var geoTypesToTarget = new List<GeometryType>();
            if (factory.Settings.edgesTargeted)
            {
                geoTypesToTarget.Add(GeometryType.Lines);
            }
            GameObject geoVision = factory.CreateGeometryVision(position, Quaternion.identity,  geoTypesToTarget,  true);
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
            EditorBuildSettingsScene[] originalScenes = AddSceneToBuildSettings(scenePath);

            return originalScenes;
        }
 
        public static void SetupScene(string scenePath)
        {
            Time.timeScale = 100f;
            CleanUpEntities();
            SceneManager.LoadScene(scenePath, LoadSceneMode.Single);
        
        }
        
        public static void SetupScene(string scenePath, float timeScale)
        {
            Time.timeScale = timeScale;
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
            
            var scene = new EditorBuildSettingsScene(scenePath, true);

            testScenes.Add(scene);
            EditorBuildSettings.scenes = testScenes.ToArray();

            return originalScenes;
        }

        public static  EditorBuildSettingsScene[] SetupBuildSettings(List<string> getScenesFromPathList)
        {
            EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
            List<EditorBuildSettingsScene> testScenes = originalScenes.ToList();
        
        
            foreach (var scenePath in getScenesFromPathList)
            {
                var scene = new EditorBuildSettingsScene(scenePath, true);
                testScenes.Add(scene);
            }

            EditorBuildSettings.scenes = testScenes.ToArray();
            if ( EditorBuildSettings.scenes.Length !=0)
            {
                foreach (var editorBuildSettingsScene in EditorBuildSettings.scenes)
                {
                    Debug.Log(editorBuildSettingsScene.path);
                }

            }        
            
            var returnValues =originalScenes;

            return returnValues;
        }
    
        public static void PostCleanUpBuildSettings(EditorBuildSettingsScene[] originalScenes)
        {
            Time.timeScale = 1.0f;
            EditorSceneManager.LoadScene(0, LoadSceneMode.Single);
            EditorBuildSettings.scenes = originalScenes;
        }
        
        public static void CleanUpEntities()
        {
            var allWorlds = World.All;
            foreach (var world in allWorlds)
            {
                var entityManager = world.EntityManager;
                foreach (var e in entityManager.GetAllEntities())
                    entityManager.DestroyEntity(e);
            }

        }
    }
}