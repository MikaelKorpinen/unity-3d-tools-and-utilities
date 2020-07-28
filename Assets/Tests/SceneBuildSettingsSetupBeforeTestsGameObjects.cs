using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using Plugins.GeometricVision;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    #if UNITY_EDITOR
    public class SceneBuildSettingsSetupBeforeTestsGameObjects : IPrebuildSetup
    {
        public void Setup()
        {
            TestSessionVariables.BuildScenes = TestUtilities.SetupBuildSettings(GetScenesFromPathGameObjects());
        }
        
        List<string> GetScenesFromPathGameObjects()
        {
            var scenePaths = TestUtilities.CreateScenePathFromRelativeAddress("Tests/TestScenes/");

            return scenePaths;
        }
    }
    public class SceneBuildSettingsSetupBeforeTestsEntities : IPrebuildSetup
    {
        public void Setup()
        {
            TestSessionVariables.BuildScenes = TestUtilities.SetupBuildSettings(GetScenesForEntitiesFromPath());
        }
        
        List<string>  GetScenesForEntitiesFromPath()
        {
            var scenePaths = TestUtilities.CreateScenePathFromRelativeAddress("Tests/TestScenesEntities/");

            return scenePaths;
        }
    }
    internal static class TestSessionVariables
    {
        internal static EditorBuildSettingsScene[] BuildScenes;
    }

#endif
}
