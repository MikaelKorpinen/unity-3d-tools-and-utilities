using System.Collections;
using System.Collections.Generic;
using GeometricVision;
using Plugins.GeometricVision;
using UnityEngine;

public class GeometryVisionMemory 
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
