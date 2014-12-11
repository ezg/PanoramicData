using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PanoramicDataService.Apis
{
    public class DataController : ApiController
    {
        public string GetRunQuery([FromUri] string query)
        {
            return executeQuery(query);
        }

        public string PostRunQuery([FromBody] string query)
        {
            return executeQuery(query);
        }

        private string executeQuery(string query) 
        {
            SqlConnection connection = new SqlConnection(
                "Server=tcp:nog7tpedho.database.windows.net,1433;Database=panoramicdata;User ID=panoramicdata@nog7tpedho;Password=Browngfx1;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;");
            connection.Open();
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;

            string ret = "";
            SqlDataReader myReader = cmd.ExecuteReader();
            while (myReader.Read())
            {
                for (int field = 0; field < myReader.FieldCount; field++)
                {
                    ret += myReader[myReader.GetName(field)].ToString() + "\t";
                }
                ret +="\n";
            }

            return ret;
        }




    }
}
