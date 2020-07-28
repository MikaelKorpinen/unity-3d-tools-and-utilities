using System;
using System.Collections;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.TestScriptsForGameObjects
{
    public class TargetingTests 
    {
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
                
        private readonly GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processGameObjects = true,
            processGameObjectsEdges = false,
            edgesTargeted = false,
            defaultTargeting = true,
        };

        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultPerformanceTests)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsGameObjects))]
        public IEnumerator TargetingSystemGetsAddedIfTargetingEnabled([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForGameObjectsFromPath))] string scenePath)
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
    }
}
