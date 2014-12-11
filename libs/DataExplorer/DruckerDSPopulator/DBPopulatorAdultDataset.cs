using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DruckerDSPopulator
{
    /*
     * delete from census.census where id in (
    select top 6281 id from census.census order by NEWID() )
    update census.census set employer_type = 'Federal-Govt' where employer_type = 'Federal-gov';
    update census.census set employer_type = 'Other-Govt' where employer_type = 'Local-gov';
    update census.census set employer_type = 'Other-Govt' where employer_type = 'State-gov';
    update census.census set employer_type = 'Private' where employer_type = 'Private';
    update census.census set employer_type = 'Self-Employed' where employer_type = 'Self-emp-inc';
    update census.census set employer_type = 'Self-Employed' where employer_type = 'Self-emp-not-inc';
    update census.census set employer_type = 'Not-Working' where employer_type = 'Without-pay';
    update census.census set employer_type = 'Not-Working' where employer_type = 'Never-worked';
    update census.census set employer_type = NULL where employer_type = '?';

    update census.census set occupation = 'Admin' where occupation = 'Adm-clerical';
    update census.census set occupation = 'Military' where occupation = 'Armed-Forces';
    update census.census set occupation = 'Blue-Collar' where occupation = 'Craft-repair';
    update census.census set occupation = 'White-Collar' where occupation = 'Exec-managerial';
    update census.census set occupation = 'Blue-Collar' where occupation = 'Farming-fishing';
    update census.census set occupation = 'Blue-Collar' where occupation = 'Handlers-cleaners';
    update census.census set occupation = 'Blue-Collar' where occupation = 'Machine-op-inspct';
    update census.census set occupation = 'Service' where occupation = 'Other-service';
    update census.census set occupation = 'Service' where occupation = 'Priv-house-serv';
    update census.census set occupation = 'Professional' where occupation = 'Prof-specialty';
    update census.census set occupation = 'Other-Occupations' where occupation = 'Protective-serv';
    update census.census set occupation = 'Sales' where occupation = 'Sales';
    update census.census set occupation = 'Other-Occupations' where occupation = 'Tech-support';
    update census.census set occupation = 'Blue-Collar' where occupation = 'Transport-moving';
    update census.census set occupation = NULL where occupation = '?';

    update census.census set race = 'White' where race = 'White';
    update census.census set race = 'Black' where race = 'Black';
    update census.census set race = 'Native American' where race = 'Amer-Indian-Eskimo';
    update census.census set race = 'Asian' where race = 'Asian-Pac-Islander';
    update census.census set race = 'Other' where race = 'Other';
    update census.census set race = NULL where race = '?';

    update census.census set education = 'Dropout' where education = '10th';
    update census.census set education = 'Dropout' where education ='11th';
    update census.census set education = 'Dropout' where education ='12th';
    update census.census set education = 'Dropout' where education ='1st-4th';
    update census.census set education = 'Dropout' where education ='5th-6th';
    update census.census set education = 'Dropout' where education ='7th-8th';
    update census.census set education = 'Dropout' where education ='9th';
    update census.census set education = 'Associates' where education ='Assoc-acdm';
    update census.census set education = 'Associates' where education ='Assoc-voc';
    update census.census set education = 'Bachelors' where education ='Bachelors';
    update census.census set education = 'Doctorate' where education ='Doctorate';
    update census.census set education = 'HS-Graduate' where education ='HS-Grad';
    update census.census set education = 'Masters' where education ='Masters';
    update census.census set education = 'Dropout' where education ='Preschool';
    update census.census set education = 'Prof-School' where education ='Prof-school';
    update census.census set education = 'HS-Graduate' where education ='Some-college';
    update census.census set education = NULL where education = '?';


    update census.census set marital_status = 'Never-Married' where marital_status='Never-married';
    update census.census set marital_status = 'Married' where marital_status='Married-AF-spouse';
    update census.census set marital_status = 'Married' where marital_status='Married-civ-spouse';
    update census.census set marital_status = 'Not-Married' where marital_status='Married-spouse-absent';
    update census.census set marital_status = 'Not-Married' where marital_status='Separated';
    update census.census set marital_status = 'Not-Married' where marital_status='Divorced';
    update census.census set marital_status = 'Widowed' where marital_status='Widowed';

    update census.census set native_country = NULL where native_country = '?';
    update census.census set native_country = 'United-States' where native_country = 'Outlying-US(Guam-USVI-etc)';
    */

    class DBPopulatorAdultDataset
    {
        public static SqlConnection connection = null;

        public static void Populate()
        {
            SqlConnectionStringBuilder stringBuilder = new SqlConnectionStringBuilder(
               @"Server=tcp:gari-srv.cs.brown.edu,21564\HUMBUBCENTRAL;Database=PanoramicData;User ID=test;Password=P@55word;Connection Timeout=30;");
            
            
            connection = new SqlConnection(stringBuilder.ToString());
            connection.Open();

            createTables();

            Stream stream = File.Open(@"C:\Users\ez\Downloads\adult.csv", FileMode.Open);
            int nextSales = 0;

            Dictionary<string, string> states = new Dictionary<string, string>();

            SqlCommand cmd = null;
            cmd = connection.CreateCommand();

            using (StreamReader sr = new StreamReader(stream))
            {
                String line = null;

                while ((line = sr.ReadLine()) != null)
                {
                    int id = nextSales++;
                    
                    List<string> entries = CSVHelper.CSVLineSplit(line);
                    if (entries.Count < 12)
                    {
                        continue;
                    }

                    cmd .CommandText += string.Format(
                        "INSERT INTO census.census (" +
                                    "id," +
                                    "age," +
                                    "employer_type," +
                                    "education," +
                                    "marital_status," +
                                    "occupation," +
                                    "relationship," +
                                    "race," +
                                    "sex," +
                                    "capital_gain," +
                                    "capital_loss," +
                                    "hours_per_week," +
                                    "native_country," +
                                    "salary_over_50k)" +
                            "VALUES (" +
                                    "@id_{0}," +
                                    "@age_{0},\n" +
                                    "@employer_type_{0},\n" +
                                    "@education_{0},\n" +
                                    "@marital_status_{0},\n" +
                                    "@occupation_{0},\n" +
                                    "@relationship_{0},\n" +
                                    "@race_{0},\n" +
                                    "@sex_{0},\n" +
                                    "@capital_gain_{0},\n" +
                                    "@capital_loss_{0},\n" +
                                    "@hours_per_week_{0},\n" +
                                    "@native_country_{0},\n" +
                                    "@salary_over_50k_{0})", id);

                    //Console.WriteLine(DateTime.Parse(entries[0]).ToString() + " / " + entries[0]);
                    addSqlParam(cmd, "id", id, id);
                    addSqlParam(cmd, "age", id, int.Parse(entries[0]));
                    addSqlParam(cmd, "employer_type", id, entries[1].Trim());
                    addSqlParam(cmd, "education", id, entries[3].Trim());
                    addSqlParam(cmd, "marital_status", id, entries[5].Trim());
                    addSqlParam(cmd, "occupation", id, entries[6].Trim());
                    addSqlParam(cmd, "relationship", id, entries[7].Trim());
                    addSqlParam(cmd, "race", id, entries[8].Trim());
                    addSqlParam(cmd, "sex", id, entries[9].Trim());
                    addSqlParam(cmd, "capital_gain", id, int.Parse(entries[10]));
                    addSqlParam(cmd, "capital_loss", id, int.Parse(entries[11]));
                    addSqlParam(cmd, "hours_per_week", id, int.Parse(entries[12]));
                   // addSqlParam(cmd, "native_country", id, entries[13].Trim());
                    addSqlParam(cmd, "salary_over_50k", id, entries[14].Trim().Equals(">50K.") ? true : false);

                    string loc = entries[13].Trim();
                    if (loc == "?")
                    {
                        addSqlParam(cmd, "native_country", id, DBNull.Value);
                    }
                    else
                    {
                        if (states.ContainsKey(loc))
                        {
                            loc = states[loc];
                        }
                        else
                        {
                            string state = MapApi.MapAPI.CountryFromLocation(loc);
                            
                            loc = state + " (" + MapApi.MapAPI.GeoCode(state) + ")";
                            states.Add(entries[13].Trim(), loc);
                        }
                        if (states[entries[13].Trim()] == " ()")
                        {
                            addSqlParam(cmd, "native_country", id, loc);
                        }
                        else
                        {
                            addSqlParam(cmd, "native_country", id, loc);
                        }
                    }
                    
                    if (id%100 == 0)
                    {
                        cmd.ExecuteNonQuery();
                        cmd = connection.CreateCommand();
                    }
                }
            }

            cmd.ExecuteNonQuery();
        }

        private static void createTables()
        {
            dropConstraintsAndTable("census", "census.census");
            ExecuteNonQuery(
               "CREATE TABLE census.census (" +
               "id int NOT NULL," +
               "age int NULL," +
               "employer_type nvarchar(2048) NULL," +
               "education nvarchar(2048) NULL," +
               "marital_status nvarchar(2048) NULL," + 
               "occupation nvarchar(2048) NULL," + 
               "relationship nvarchar(2048) NULL," +
               "race nvarchar(2048) NULL," +
               "sex nvarchar(2048) NULL," +
               "capital_gain int NULL," +
               "capital_loss int NULL," + 
               "hours_per_week int NULL," +
               "native_country nvarchar(2048) NULL," +
               "salary_over_50k bit NULL," +
               "CONSTRAINT PK_census_census PRIMARY KEY (id))"
            );
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

        private static void dropConstraintsAndTable(string schema, string table)
        {
            SqlCommand cmd = connection.CreateCommand();

            cmd.CommandText = "SELECT " +
                            "'ALTER TABLE " + schema + ".' + OBJECT_NAME(parent_object_id) + " +
                            "' DROP CONSTRAINT ' + name " +
                            "FROM sys.foreign_keys " +
                            "WHERE referenced_object_id = object_id('" + table + "')";

            SqlDataReader re = cmd.ExecuteReader();
            string alterStatements = "";
            while (re.Read())
            {
                alterStatements += re.GetString(0) + ";";
            }
            re.Close();

            if (alterStatements != "")
            {
                cmd = connection.CreateCommand();
                cmd.CommandText = alterStatements;
                cmd.ExecuteNonQuery();
            }
            cmd = connection.CreateCommand();
            cmd.CommandText = "drop table " + table;
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(table + " does not exist");
            }
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
}
