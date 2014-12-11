using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace PanoramicDataService.Controllers
{
    public class TestController : ApiController
    {
        // GET api/test
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/test/5
        public string Get(int id)
        {
            SqlConnection connection = new SqlConnection(
                "Server=tcp:axinqmh9b9.database.windows.net,1433;" +
                "Database=AdventureWorks2012;User ID=adventure@axinqmh9b9;" +
                "Password=Browngfx1;Trusted_Connection=False;Encrypt=True;Connection Timeout=30;");
            connection.Open();
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = "select * from Person.Person";

            string ret = "";
            SqlDataReader myReader = cmd.ExecuteReader();
            while (myReader.Read())
            {
                ret += myReader["LastName"].ToString() + "\n";
            }

            return ret;
        }

        // POST api/test
        public void Post([FromBody]string value)
        {
        }

        // PUT api/test/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/test/5
        public void Delete(int id)
        {
        }
    }
}
