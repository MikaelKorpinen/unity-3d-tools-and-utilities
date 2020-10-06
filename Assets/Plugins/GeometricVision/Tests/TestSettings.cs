namespace Plugins.GeometricVision.Tests
{
    /// <summary>
    /// Holds static variables
    /// </summary>
    public static class TestSettings
    {
        public const string Version = "0.1.2";
        public const int DefaultSmallTest = 90900;
        public const int DefaultPerformanceTests = 192000;
        private const string testsFolderPath = "Plugins/GeometricVision/";
        public const string EntitiesSimpleTestsPath = testsFolderPath + "Tests/TestAssets/Scenes/Entities/SimpleTests/";
        public const string GameObjectsSimpleTestsPath = testsFolderPath + "Tests/TestAssets/Scenes/GameObjects/SimpleTests/";
        public const string GameObjectsStressTestsPath = testsFolderPath + "Tests/TestAssets/Scenes/GameObjects/StressTests/";
        public const string EntitiesStressTestsPath = testsFolderPath + "Tests/TestAssets/Scenes/Entities/StressTests/";
        public const string GameObjectsTargetingTests = testsFolderPath + "Tests/TestAssets/Scenes/GameObjects/ClosestTargetTesting/";
        public const string EntitiesTargetingTests = testsFolderPath + "Tests/TestAssets/Scenes/Entities/ClosestTargetTesting/";
        public const string EmptyScenePath = testsFolderPath + "Tests/TestAssets/Scenes/EmptyScene/";
    }
}