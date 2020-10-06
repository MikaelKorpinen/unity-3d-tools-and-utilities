using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Plugins.GeometricVision.Tests.TestScriptsForGameObjects
{
    public class TargetingTests 
    {
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
                
        private  GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = true,
            edgesTargeted = false,
            defaultTargeting = true,
            defaultTag = ""
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest,  Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator TargetingSystemGetsAddedIfTargetingEnabled([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
   
            int AmountOfTargetingSystemsRegistered = 0;
            int expectedObjectCount1 = 1;
            AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>().GetTargetingProgramsCount();;
            
            Assert.AreEqual(expectedObjectCount1, AmountOfTargetingSystemsRegistered);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator TargetingSystemGetsTarget([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            factorySettings.defaultTag = "";
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
   
            GeometryDataModels.Target target = new GeometryDataModels.Target();
            Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(); }).Run();
            
            Assert.True(target.isEntity == false);
            Assert.True(target.distanceToCastOrigin > 0);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjects))]
        public IEnumerator ClosestTargetListIsEmptyIfNothingIsSeen(
            [ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetSimpleTestScenePathsForGameObjects))]
            string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision =
                TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            Assert.True(geoVision.GetComponent<GeometryVision>().GetClosestTargets().Length > 0);
            //Move camera away so there is nothing to be seen
            geoVision.transform.position = new Vector3(34343f, 343434f, 3434343f);
            yield return null;
            Assert.True(geoVision.GetComponent<GeometryVision>().GetClosestTargets().Length == 0);
        }
        
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsTargeting))]
        public IEnumerator TaggingWorks([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            factorySettings.defaultTag = "Player";
            var notWantedTag1 = "Untagged";
            var notWantedTag2 =  "Respawn";
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>(); 
            NativeSlice<GeometryDataModels.Target> targets = new NativeArray<GeometryDataModels.Target>();
            Measure.Method(() => { targets = geoVision.GetComponent<GeometryVision>().GetClosestTargets(); }).Run();
            foreach (var target in targets)
            {
                var targeTransform =  geoVisionComponent.GetTransformBasedOnGeoHashCode(target.GeoInfoHashCode);
                if (targeTransform.name.Contains("Cube"))
                {
                    Assert.True(targeTransform.CompareTag(factorySettings.defaultTag));
                    Assert.True(!targeTransform.CompareTag(notWantedTag1));
                    Assert.True(!targeTransform.CompareTag(notWantedTag2));
                }
            }

            Assert.True(geoVisionComponent.TargetingInstructions.Count == 1);
        }
        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.DefaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupForGameObjectsTargeting))]
        public IEnumerator SystemWorksWithoutTagAdded([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetTargetingTestScenePathsForGameObjects))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }

            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
            NativeSlice<GeometryDataModels.Target> targets = new NativeArray<GeometryDataModels.Target>();
            Measure.Method(() => { targets = geoVision.GetComponent<GeometryVision>().GetClosestTargets(); }).Run();
            Assert.True(targets.Length >=3);//3 objects + possible left over by unity

        }
    }
}
