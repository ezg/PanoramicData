using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using DataExplorer;
using Npgsql;

namespace OpenERPSettingsCreator
{
    class Program
    {
        private static DbConnection conn = null;

        static void Main(string[] args)
        {
            string sConnectionString = "server=localhost;user=openpg;database=george;port=5432;password=openpgpwd;";
            conn = new NpgsqlConnection(sConnectionString);
            conn.Open();

            List<string> descriptors = new List<string>();

            // Employee Table
            TableDescriptor descriptor = createTableDescriptor("hr_employee", "Employee");
            descriptors.Add(descriptor.TableName + ".xml");

            // Department Table
            descriptor = createTableDescriptor("hr_department", "Department");
            descriptors.Add(descriptor.TableName + ".xml");

            // sale order Table
            descriptor = createTableDescriptor("sale_order", "Sale Order");
            descriptors.Add(descriptor.TableName + ".xml");

            // save list of descriptor filenames
            XmlSerializer serializer = new XmlSerializer(typeof(List<string>));
            TextWriter tw = new StreamWriter("descriptors.xml");
            serializer.Serialize(tw, descriptors);
            tw.Close();

        }

        static TableDescriptor createTableDescriptor(string tablename, string tablelabel)
        {
            TableDescriptor descriptor = new TableDescriptor();
            descriptor.TableName = tablename;
            descriptor.TableLabel = tablelabel;
            generateFields(descriptor);
            generateDescription(descriptor);
            generateDependencies(descriptor);
            saveToFile(descriptor, descriptor.TableName + ".xml");
            return descriptor;
        }

        static void generateFields(TableDescriptor descriptor)
        {
            string query = "select a.attname from pg_attribute a, pg_class c where a.attrelid = c.relfilenode and c.relname = '" + descriptor.TableName + "' and attnum > 0 and attstattarget != 0";
            
            DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = query;
            DbDataReader re = cmd.ExecuteReader();

            while (re.Read())
            {
                TableFieldDescriptor tfd = new TableFieldDescriptor();
                tfd.FieldName = re.GetString(0);
                descriptor.Fields.Add(tfd);
            }

            re.Close();
        }

        static void generateDescription(TableDescriptor descriptor)
        {
             string query =
                "select " +
                "    a.attname as field_name, d.description as header_name " +
                "from " +
                "    pg_description d, pg_class c, pg_attribute a " +
                "where " +
                "    d.objoid = c.relfilenode and " +
                "    a.attrelid = c.relfilenode and " +
                "    c.relname = '" + descriptor.TableName + "' and " +
                "    d.objsubid = a.attnum";

             DbCommand cmd = conn.CreateCommand();
             cmd.CommandText = query;
             DbDataReader re = cmd.ExecuteReader();

             while (re.Read())
             {
                 foreach (var tfd in descriptor.Fields)
                 {
                     if (tfd.FieldName == re.GetString(0))
                     {
                         tfd.FieldLabel = re.GetString(1);
                         break;
                     }
                 }
             }

             re.Close();
        }

        static void generateDependencies(TableDescriptor descriptor)
        {
            string query =
                "select " +
                "    tot.relname as to_table, " +
                "    fratt.attname as from_column_name, " +
                "    toatt.attname as to_column_name " +
                "from " +
                "    pg_constraint as cot, " +
                "    pg_class as tot, " +
                "    pg_class as frt, " +
                "    pg_attribute as fratt, " +
                "    pg_attribute as toatt " +
                "where " +
                "    frt.oid = cot.conrelid and " +
                "    tot.oid = cot.confrelid and " +
                "    cot.conkey[1] = fratt.attnum and " +
                "    cot.conrelid = fratt.attrelid and " +
                "    cot.confkey[1] = toatt.attnum and " +
                "    cot.confrelid = toatt.attrelid and " +
                "    frt.relname = '" + descriptor.TableName + "'";

             DbCommand cmd = conn.CreateCommand();
             cmd.CommandText = query;
             DbDataReader re = cmd.ExecuteReader();

             while (re.Read())
             {
                 TableDependencyDescriptor dependent = new TableDependencyDescriptor();
                 descriptor.Dependencies.Add(dependent);
                 dependent.TableDescriptor = new TableDescriptor();
                 dependent.TableDescriptor.TableName = re.GetString(0);
                 dependent.FromColumnName = re.GetString(1);
                 dependent.ToColumnName = re.GetString(2);

                 // set the label 
                 foreach (var tfd in descriptor.Fields)
                 {
                     if (tfd.FieldName == dependent.FromColumnName)
                     {
                         dependent.TableDescriptor.TableLabel = tfd.FieldLabel;
                         break;
                     }
                 }
             }
             re.Close();

             foreach (var dependent in descriptor.Dependencies)
             {
                 generateFields(dependent.TableDescriptor);
                 generateDescription(dependent.TableDescriptor);
             }
        }

        static void saveToFile(TableDescriptor descriptor, string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TableDescriptor));
            TextWriter tw = new StreamWriter(filename);
            serializer.Serialize(tw, descriptor);
            tw.Close();
        }

        static TableDescriptor loadFromFile(string filename)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(TableDescriptor));
            TextReader tr = new StreamReader(filename);
            TableDescriptor descriptor = (TableDescriptor)deserializer.Deserialize(tr);
            tr.Close();
            return descriptor;
        }
    }
}
