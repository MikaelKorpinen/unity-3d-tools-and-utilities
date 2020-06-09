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

namespace Tests
{
    public class FrustumTestsObjects
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
        [Timeout(2000)]
        public IEnumerator GameObjectIsPickedIfInsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++){yield return null;}

            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            yield return null;
            GameObject geoVision= factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25, GeometryType.Edges, 0);
            yield return null;
            var geoEye = geoVision.GetComponent<GeometryVisionEye>();
            Assert.AreEqual(expectedObjectCount, geoEye.SeenObjects.Count);
        }


        [UnityTest, Performance, Version(version)]
        [Timeout(2000)]
        public IEnumerator GameObjectIsRemovedAndAddedBackIfOutsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {

            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++){yield return null;}
            
            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            int expectedObjectCount2 = 0;
            int expectedObjectCount3 = expectedObjectCount;
            
            GameObject geoVision= factoryAndOriginalScenes.Item1.CreateGeometryVision(new Vector3(0f, 0f, -6f), Quaternion.identity, 25, GeometryType.Edges, 0);
            yield return null;
            
            Assert.AreEqual(expectedObjectCount, geoVision.GetComponent<GeometryVisionEye>().SeenObjects.Count);  

            geoVision.transform.position = new Vector3(10f,10f,10);//Move Object outside the cube
            
            geoVision.GetComponent<GeometryVisionEye>().RegenerateVisionArea(25);
            
            yield return null;         
            
            Assert.AreEqual(expectedObjectCount2, geoVision.GetComponent<GeometryVisionEye>().SeenObjects.Count);
            
            geoVision.transform.position = new Vector3(0f,0f,-6f);//Move Object back to the cube
            yield return null;
            Assert.AreEqual(expectedObjectCount3, geoVision.GetComponent<GeometryVisionEye>().SeenObjects.Count);
        }
    }
}
