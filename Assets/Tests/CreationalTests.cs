using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Testsun
{
    public class CreationalTests : MonoBehaviour
    {
        private const string version = TestSettings.Version;
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
        
        [TearDown]
        public void TearDown()
        {
            //Put original scenes back to build settings
            EditorBuildSettings.scenes = factoryAndOriginalScenes.Item2;
        }
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GeometryVisionGetsCreatedWithoutFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var go = Instantiate(new GameObject("geoVision"));
            go.transform.position = new Vector3(0f,0f,-6f);
            
            //TODO: Find out how to measure stuff happening in the start and awake from here to get the real estimate

            go.AddComponent<GeometryVisionEye>();

            
            yield return null;

            int amountOfObjectsInScene = 0;
            int expectedObjectCount1 = TestUtilities.GetObjectCountFromScene();
            

            amountOfObjectsInScene = go.GetComponent<GeometryVisionEye>().ControllerBrain.CountSceneObjects();
            yield return null;
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GeometryVisionGetsCreatedWithFactory([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            int expectedObjectCount1 = FindObjectsOfType<GameObject>().Length;
            GameObject geoVision = factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25, GeometryType.Edges, 0);


            yield return null;
            int amountOfObjectsInScene = 0;

            amountOfObjectsInScene = geoVision.GetComponent<GeometryVisionEye>().ControllerBrain.CountSceneObjects();
            Debug.Log("total objects: " + amountOfObjectsInScene);
            Assert.AreEqual(expectedObjectCount1, amountOfObjectsInScene);
        }


    }
}