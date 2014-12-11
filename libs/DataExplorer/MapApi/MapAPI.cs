using MapApi.BingMapService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapApi
{
    public class MapAPI
    {
        public static string BingMapKey = "AvEXTJAXOlPdAHWwrAm8tQ8CCiyK3DtKVy1RDHUVfThltB9w2ywRdKVV_e5ccvSZ";

        public static string GeoCode(string loc)
        {
            if (loc == "")
            {
                return "";
            }
            GeocodeRequest geocodeRequest = new GeocodeRequest();

            geocodeRequest.Credentials = new MapApi.BingMapService.Credentials();
            geocodeRequest.Credentials.ApplicationId = BingMapKey;

            geocodeRequest.Query = loc;

            //Set the opeionts to only return high confidence results
            ConfidenceFilter[] filters = new ConfidenceFilter[1];
            filters[0] = new ConfidenceFilter();
            filters[0].MinimumConfidence = Confidence.Low;

            //Add the filters to the options
            GeocodeOptions geocodeOptions = new GeocodeOptions();
            //geocodeOptions.Filters = new List<FilterBase>(filters);
            geocodeRequest.Options = geocodeOptions;

            //Make the geocode request
            GeocodeServiceClient geoClient = new GeocodeServiceClient("BasicHttpBinding_IGeocodeService");
            try
            {
                GeocodeResponse response = geoClient.Geocode(geocodeRequest);
                return response.Results[0].Locations[0].Longitude + ", " + response.Results[0].Locations[0].Latitude;
            }
            catch (Exception e)
            {
                return "";
            }

        }

        public static string CountryFromLocation(string loc)
        {
            if (loc == "")
            {
                return "";
            }
            GeocodeRequest geocodeRequest = new GeocodeRequest();

            geocodeRequest.Credentials = new MapApi.BingMapService.Credentials();
            geocodeRequest.Credentials.ApplicationId = BingMapKey;

            geocodeRequest.Query = loc;

            //Set the opeionts to only return high confidence results
            ConfidenceFilter[] filters = new ConfidenceFilter[1];
            filters[0] = new ConfidenceFilter();
            filters[0].MinimumConfidence = Confidence.High;

            //Add the filters to the options
            GeocodeOptions geocodeOptions = new GeocodeOptions();
            geocodeOptions.Filters = new List<FilterBase>(filters);
            geocodeRequest.Options = geocodeOptions;

            //Make the geocode request
            GeocodeServiceClient geoClient = new GeocodeServiceClient("BasicHttpBinding_IGeocodeService");
            try
            {
                GeocodeResponse response = geoClient.Geocode(geocodeRequest);
                //return response.Results[0].Locations[0].Longitude + ", " + response.Results[0].Locations[0].Latitude;
                return response.Results[0].Address.CountryRegion;
            }
            catch (Exception e)
            {
                return "";
            }

        }

        public static string StateFromLocation(string loc)
        {
            if (loc == "")
            {
                return "";
            }
            GeocodeRequest geocodeRequest = new GeocodeRequest();

            geocodeRequest.Credentials = new MapApi.BingMapService.Credentials();
            geocodeRequest.Credentials.ApplicationId = BingMapKey;

            geocodeRequest.Query = loc;

            //Set the opeionts to only return high confidence results
            ConfidenceFilter[] filters = new ConfidenceFilter[1];
            filters[0] = new ConfidenceFilter();
            filters[0].MinimumConfidence = Confidence.High;

            //Add the filters to the options
            GeocodeOptions geocodeOptions = new GeocodeOptions();
            geocodeOptions.Filters = new List<FilterBase>(filters);
            geocodeRequest.Options = geocodeOptions;

            //Make the geocode request
            GeocodeServiceClient geoClient = new GeocodeServiceClient("BasicHttpBinding_IGeocodeService");
            try
            {
                GeocodeResponse response = geoClient.Geocode(geocodeRequest);
                //return response.Results[0].Locations[0].Longitude + ", " + response.Results[0].Locations[0].Latitude;
                return response.Results[0].Address.AdminDistrict;
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }
}
