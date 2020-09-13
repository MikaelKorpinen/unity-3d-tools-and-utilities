namespace Plugins.GeometricVision
{
        /*
         * Global settings for all the components
         */
        public static class GeometryVisionSettings
        {
            //These are ignored when processing transforms
            public static string NameOfMainEffect { get; } = "GVTMainActionEffect";
            public static string NameOfEndEffect { get; } = "GVTEndActionEffect";
            public static string NameOfStartingEffect { get; } = "GVTStartActionEffect";
            public static string ManagerName { get; set; } = "GeometryVisionManager";

            public static string HeaderImagePath { get; set; } =
                "/Plugins/GeometricVision/UI/Images/GeoVisionTargeting.png";

            public static string NewActionsAssetForTargetingPath { get; set; } =
                "Assets/NewActionsAssetForTargeting.asset";
        }
}