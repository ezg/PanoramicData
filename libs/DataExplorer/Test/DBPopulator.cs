using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Test
{    
    class DBPopulator
    {
        public static SqlConnection connection = null;

        public static void Populate()
        {
            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
               @"Server=MACBOOK-WIN8\SQLEXPRESS;Database=PanoramicData;Connection Timeout=30;Max Pool Size=200;Trusted_Connection=Yes;");
            
            
            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();

            Stream stream = File.Open(@"C:\ez_projects\HumBub\starPad SDK\Apps\DataExplorer\Test\Book1.csv", FileMode.Open);
            int nextTip = 0;

            SqlCommand cmd = null;
            // obs, totbill, tip, sex, smoker, day, time, size
            using (StreamReader sr = new StreamReader(stream))
            {
                String line = "";

                while ((line = sr.ReadLine()) != null)
                {
                    List<string> entries = CSVHelper.CSVLineSplit(line);

                    string embarked = entries[1];
                    string home = entries[2];
                    string destination = entries[3];

                    string newEmbarked = null;
                    string newHome = null;
                    string newDestination = null;

                    if (embarked != "NULL")
                    {
                        string loc = embarked.Substring(0, embarked.IndexOf("("));
                        string country = MapApi.MapAPI.CountryFromLocation(loc);
                        newEmbarked = country + " (" + MapApi.MapAPI.GeoCode(country) + ")";

                        cmd = connection.CreateCommand(); if (newEmbarked == " ()")
                        {
                            cmd.CommandText += "update titanic.passenger set embarked = null where id = " + entries[0] + ";\n";
                        }
                        else
                        {
                            cmd.CommandText += "update titanic.passenger set embarked = '" + newEmbarked + "' where id = " + entries[0] + ";\n";
                        }
                        cmd.ExecuteNonQuery();
                    }

                    if (home != "NULL")
                    {
                        string loc = home.Substring(0, home.IndexOf("("));
                        string country = MapApi.MapAPI.CountryFromLocation(loc);
                        newHome = country + " (" + MapApi.MapAPI.GeoCode(country) + ")";  

                        cmd = connection.CreateCommand();
                        if (newHome == " ()")
                        {
                            cmd.CommandText += "update titanic.passenger set home = null where id = " + entries[0] + ";\n";
                        }
                        else 
                        {
                            cmd.CommandText += "update titanic.passenger set home = '" + newHome + "' where id = " + entries[0] + ";\n";
                        }
                        cmd.ExecuteNonQuery();
                    }

                    if (destination != "NULL")
                    {
                        string loc = destination.Substring(0, destination.IndexOf("("));
                        string country = MapApi.MapAPI.CountryFromLocation(loc);
                        newDestination = country + " (" + MapApi.MapAPI.GeoCode(country) + ")";

                        cmd = connection.CreateCommand();
                        if (newDestination == " ()")
                        {
                            cmd.CommandText += "update titanic.passenger set destination = null where id = " + entries[0] + ";\n";
                        }
                        else
                        {
                            cmd.CommandText += "update titanic.passenger set destination = '" + newDestination + "' where id = " + entries[0] + ";\n";
                        }
                        cmd.ExecuteNonQuery();
                    }
                    Console.WriteLine("=--- : "+ entries[0]);
                    Console.WriteLine(newEmbarked);
                    Console.WriteLine(newHome);
                    Console.WriteLine(newDestination);
                    //cmd.ExecuteNonQuery();
                }
            }
        }


        private static List<List<object>> ExecuteQuery(string query, params object[] parameters)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            if (parameters != null && parameters.Length > 0)
            {
                for (int i = 0; i < parameters.Length; i += 2)
                {
                    cmd.Parameters.Add(new SqlParameter(parameters[i].ToString(), parameters[i + 1]));
                }
            }
            SqlDataReader myReader = cmd.ExecuteReader();

            List<List<object>> ret = new List<List<object>>();
            int count = 0;
            while (myReader.Read())
            {
                List<object> row = new List<object>();

                for (int field = 0; field < myReader.FieldCount; field++)
                {
                    row.Add(myReader[myReader.GetName(field)]);
                }
                ret.Add(row);

                count++;
            }
            myReader.Close();

            return ret;
        }

        private static void ExecuteNonQuery(string query)
        {
            SqlCommand cmd = connection.CreateCommand();
            cmd.CommandText = query;
            cmd.ExecuteNonQuery();
        }


        private static void addSqlParam(SqlCommand cmd, string paramName, int id, object param, bool insertZeroInstedOfNull = false)
        {
            if (param == null)
            {
                if (insertZeroInstedOfNull)
                {
                    cmd.Parameters.Add(new SqlParameter("@" + paramName + "_" + id, float.Parse("0")));
                }
                else
                {
                    cmd.Parameters.Add(new SqlParameter("@" + paramName + "_" + id, DBNull.Value));
                }
            }
            else
            {
                cmd.Parameters.Add(new SqlParameter("@" + paramName + "_" + id, param));
            }
        }
    }

    public class CSVHelper
    {
        public static List<string> CSVLineSplit(string line)
        {
            bool open = false;
            List<string> words = new List<string>();
            string word = "";
            for (int i = 0; i < line.Length; i++)
            {
                if (line[i] == ',' && !open)
                {
                    words.Add(word);
                    word = "";
                }
                else if (line[i] == '"' && !open)
                {
                    open = true;
                    //words.Add(word);
                    //word = "";
                }
                else if (line[i] == '"' && open)
                {
                    open = false;
                }
                else
                {
                    word += line[i];
                }
            }
            words.Add(word);
            return words;
        }
    }
}
