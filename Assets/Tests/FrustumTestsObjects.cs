using System;
using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.Interfaces.Implementations;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
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
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GameObjectIsPickedIfInsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {
            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++){yield return null;}

            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            yield return null;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes, false);
            yield return null;
            var geoEye = geoVision.GetComponent<GeometryVisionEye>();
            Assert.AreEqual(expectedObjectCount, geoEye.seenTransforms.Count);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        public IEnumerator GameObjectIsRemovedAndAddedBackIfOutsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesFromPath))] string scenePath)
        {

            factoryAndOriginalScenes = TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++){yield return null;}
            
            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            int expectedObjectCount2 = 0;
            int expectedObjectCount3 = expectedObjectCount;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), factoryAndOriginalScenes, false);
            yield return null;
            
            Assert.AreEqual(expectedObjectCount, geoVision.GetComponent<GeometryVisionEye>().seenTransforms.Count);  

            geoVision.transform.position = new Vector3(10f,10f,10);//Move Object outside the cube
            
            geoVision.GetComponent<GeometryVision>().RegenerateVisionArea(25);
            
            yield return null;         
            
            Assert.AreEqual(expectedObjectCount2, geoVision.GetComponent<GeometryVisionEye>().seenTransforms.Count);
            
            geoVision.transform.position = new Vector3(0f,0f,-6f);//Move Object back to the cube
            yield return null;
            Assert.AreEqual(expectedObjectCount3, geoVision.GetComponent<GeometryVisionEye>().seenTransforms.Count);
        }
    }
}
