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

namespace Tests.TestScriptsForGameObjects
{
    public class TargetingTests 
    {
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
                
        private  GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = true,
            processGameObjectsEdges = false,
            edgesTargeted = false,
            defaultTargeting = true,
            defaultTag = ""
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
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
            Measure.Method(() => { AmountOfTargetingSystemsRegistered = geoVision.GetComponent<GeometryTargetingSystemsContainer>().GetTargetingProgramsCount(); }).Run();

            Debug.Log("total targeting systems: " + AmountOfTargetingSystemsRegistered);
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
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;
   
            GeometryDataModels.Target target = new GeometryDataModels.Target();
            Measure.Method(() => { target = geoVision.GetComponent<GeometryVision>().GetClosestTarget(false); }).Run();

            Debug.Log("found targeting system: " + target);
                        
            Assert.True(target.isEntity == false);
            Assert.True(target.distanceToCastOrigin > 0);
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
            List<GeometryDataModels.Target> targets = new List<GeometryDataModels.Target>();
            Measure.Method(() => { targets = geoVision.GetComponent<GeometryVision>().GetClosestTargets(); }).Run();
            foreach (var target in targets)
            {
                Debug.Log("found targeting system: " + target);
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
            var geoVisionComponent = geoVision.GetComponent<GeometryVision>(); 
            List<GeometryDataModels.Target> targets = new List<GeometryDataModels.Target>();
            Measure.Method(() => { targets = geoVision.GetComponent<GeometryVision>().GetClosestTargets(); }).Run();
            Assert.True(targets.Count >=3);//3 objects + possible left over by unity

        }
    }
}
