namespace Plugins.GeometricVision
{
        /*
         * Global settings for all the components
         * Put only permanent stuff here
         */
        public static class GeometryVisionSettings
        {
            //These are ignored when processing transforms
            public static string NameOfMainEffect { get; } = "GVTMainActionEffect";
            public static string NameOfEndEffect { get; } = "GVTEndActionEffect";
            public static string NameOfStartingEffect { get; } = "GVTStartActionEffect";
            public static string ManagerName { get; set; } = "GeometryVisionManager";

            public static string HeaderImagePath { get; set; } =
                "/Plugins/GeometricVision/Editor/UI/Images/GeoVisionTargeting.png";

            public static string NewActionsAssetForTargetingPath { get; set; } =
                "Assets/Plugins/GeometricVision/NewActionsAssetForTargeting.asset";
            
        }
}