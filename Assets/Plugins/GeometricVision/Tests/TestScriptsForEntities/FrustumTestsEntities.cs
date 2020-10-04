using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using Unity.Entities;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForEntities
{
    public class FrustumTestsEntities
    {
        private const string version = TestSettings.Version;
        
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
        GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = false,
            processEntities = true,
            defaultTargeting = true,
        };
        
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
            TestUtilities.CleanUpEntities();
        }

        /// <summary>
        /// Test transform culling. Transform that may or may not have renderer/mesh/bounding box in it
        /// </summary>
        /// <param name="scenePath"></param>
        /// <returns></returns>
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator GameObjectIsPickedIfInsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            var world = World.DefaultGameObjectInjectionWorld;
            var testingUtilities = world.CreateSystem<EntityTestingUtilities>();
            for (int i = 0; i < 50; i++){yield return null;}
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var expectedObjectCount = testingUtilities.CountEntities();


            var seenCount = geoVision.GetComponent<GeometryVision>().GetClosestTargets().Where(target => target.isSeen)
                .ToList().Count;
      
            Assert.AreEqual(expectedObjectCount,seenCount );
        }
        
        /// <summary>
        /// Test transform culling. Transform that may or may not have renderer/mesh/bounding box in it
        /// </summary>
        /// <param name="scenePath"></param>
        /// <returns></returns>
        [UnityTest, Performance, Version(version)]
        [Timeout(TestSettings.DefaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForEntities))]
        public IEnumerator GameObjectIsRemovedAndAddedBackIfOutsideFrustum([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForEntities))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            var world = World.DefaultGameObjectInjectionWorld;
            var testingUtilities = world.CreateSystem<EntityTestingUtilities>();
            for (int i = 0; i < 50; i++){yield return null;}
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var expectedObjectCount = testingUtilities.CountEntities();
            
            int expectedObjectCount2 = 0;
            int expectedObjectCount3 = expectedObjectCount;
            
            var seenCount = geoVision.GetComponent<GeometryVision>().GetClosestTargets().Where(target => target.isSeen)
                .ToList().Count;
            Assert.AreEqual(expectedObjectCount, seenCount);  

            geoVision.transform.position = new Vector3(10f,10f,10);//Move Object outside the cube
            yield return null;         
            seenCount = geoVision.GetComponent<GeometryVision>().GetClosestTargets().Where(target => target.isSeen)
                .ToList().Count;
            Assert.AreEqual(expectedObjectCount2, seenCount);
            
            geoVision.transform.position = new Vector3(0f,0f,-6f);//Move Object back to the cube
            yield return null;
            seenCount = geoVision.GetComponent<GeometryVision>().GetClosestTargets().Where(target => target.isSeen)
                .ToList().Count;
            Assert.AreEqual(expectedObjectCount3, seenCount);
        }
    }
}
