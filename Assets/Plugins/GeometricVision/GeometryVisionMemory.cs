using System.Collections.Generic;

namespace Plugins.GeometricVision
{
     internal class GeometryVisionMemory 
     {
          private List<GeometryDataModels.GeoInfo> _geoInfos = new List<GeometryDataModels.GeoInfo>();

          public GeometryVisionMemory()
          {
               GeoInfos = new List<GeometryDataModels.GeoInfo>();
          }

          public List<GeometryDataModels.GeoInfo> GeoInfos
          {
               get { return _geoInfos; }
               set { _geoInfos = value; }
          }
     }
}
