using System;
using System.Collections;
using GeometricVision;
using NUnit.Framework;
using Plugins.GeometricVision;
using Plugins.GeometricVision.ImplementationsEntities;
using Plugins.GeometricVision.Interfaces.ImplementationsEntities;
using Unity.PerformanceTesting;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.TestScriptsForEntities
{
    public class TargetingTestsEntities : MonoBehaviour
    {
        private Tuple<GeometryVisionFactory, EditorBuildSettingsScene[]> factoryAndOriginalScenes;
        
        private readonly GeometryDataModels.FactorySettings factorySettings = new GeometryDataModels.FactorySettings
        {
            fielOfView =  25f,
            processEntities = true,
            processGameObjectsEdges = false,
            defaultTargeting = true,
        };
        
        [TearDown]
        public void TearDown()
        {
            TestUtilities.PostCleanUpBuildSettings(TestSessionVariables.BuildScenes);
        }

        [UnityTest, Performance, Version(TestSettings.Version)]
        [Timeout(TestSettings.defaultSmallTest)]
        [PrebuildSetup(typeof(SceneBuildSettingsSetupBeforeTestsEntities))]
        public IEnumerator TargetingSystemGetsAddedIfTargetingEnabled([ValueSource(typeof(TestUtilities), nameof(TestUtilities.GetScenesForEntitiesFromPath))] string scenePath)
        {
            TestUtilities.SetupScene(scenePath);
            for (int i = 0; i < 50; i++)
            {
                yield return null;
            }
            var geoVision = TestUtilities.SetupGeoVision(new Vector3(0f, 0f, -6f), new GeometryVisionFactory(factorySettings));
            yield return null;

            bool isAdded = geoVision.GetComponent<GeometryTargetingSystemsContainer>().GetTargetingProgram<GeometryEntitiesObjectTargeting>() != null;
            
            Assert.True(isAdded);
        }
    }
}
