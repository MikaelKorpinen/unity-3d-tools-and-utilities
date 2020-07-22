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
        GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = true,
            processGameObjectsEdges = false,
        };
        
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsGameObjects))]
        public IEnumerator GameObjectIsPickedIfInsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForGameObjectsFromPath))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++){yield return null;}

            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            yield return null;
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;

            var geoEye = geoVision.GetComponent<GeometryVision>().GetEye<GeometryVisionEye>();
            Assert.AreEqual(expectedObjectCount, geoEye.seenTransforms.Count);
        }

        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsGameObjects))]
        public IEnumerator GameObjectIsRemovedAndAddedBackIfOutsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForGameObjectsFromPath))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 25; i++){yield return null;}
            
            var expectedObjectCount = TestUtilities.GetObjectCountFromScene(Vector3.zero);
            int expectedObjectCount2 = 0;
            int expectedObjectCount3 = expectedObjectCount;

            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>();
            var geoEye = geoVisionComponent.GetEye<GeometryVisionEye>();
            Assert.AreEqual(expectedObjectCount, geoEye.seenTransforms.Count);  

            geoVision.transform.position = new Vector3(10f,10f,10);//Move Object outside the cube
            yield return null;         

            Assert.AreEqual(expectedObjectCount2, geoEye.seenTransforms.Count);
            
            geoVision.transform.position = new Vector3(0f,0f,-6f);//Move Object back to the cube
            yield return null;
            Assert.AreEqual(expectedObjectCount3, geoEye.seenTransforms.Count);
        }
    }
}
