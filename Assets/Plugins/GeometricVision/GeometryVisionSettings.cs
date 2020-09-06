namespace GeometricVision
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
        }
}