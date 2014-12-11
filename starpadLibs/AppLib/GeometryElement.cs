using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace starPadSDK.AppLib
{
    public interface GeometryElement
    {
        GeoAPI.Geometries.IGeometry GetGeometry();
    }
}
