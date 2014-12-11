using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using starPadSDK.Geom;
using starPadSDK.MathExpr;

namespace Serialization
{
    public class Serializer
    {
        // Allowd format types
        private static Type[] FormatTypes = new Type[] 
        { 
            typeof(ShapeModel), 
            typeof(ImageModel),
            typeof(TextModel),
            typeof(StroqModel),
            typeof(TableModel),
            typeof(BubbleModel),
            typeof(Expr),
            typeof(ChartModel)
        };

        public static void ExportToPowerPoint(string filename, string templateFilename, Rct viewPointRct, List<Model> models)
        {
            PowerPointSerializer pps = new PowerPointSerializer();
            pps.Serialize(filename, templateFilename, viewPointRct, models); 
        }

        public static void ExportToFile(string filename, List<Model> models)
        {
            Stream stream = File.Open(filename, FileMode.Create);
            ExportToStream(stream, models);
            stream.Close();
        }

        public static List<Model> LoadFromFile(string filename)
        {
            Stream stream = File.Open(filename, FileMode.Create);
            List<Model> models = LoadFromStream(stream);
            stream.Close();
            return models;
        }

        public static void ExportToStream(Stream stream, List<Model> models)
        {
            /*if (fileFormat == FileFormat.XML)
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<Model>), FormatTypes);
                TextWriter tw = new StreamWriter(stream);
                serializer.Serialize(tw, models);
            }
            else if (fileFormat == FileFormat.Binary)*/
            //{
            try
            {
                BinaryFormatter bFormatter = new BinaryFormatter();
                bFormatter.Serialize(stream, models);
            }
            catch (Exception ee)
            {
            }
            //}
        }

        public static List<Model> LoadFromStream(Stream stream)
        {
            List<Model> models = new List<Model>();

            /*if (fileFormat == FileFormat.XML)
            {
                XmlSerializer deserializer = new XmlSerializer(typeof(List<Model>), FormatTypes);
                TextReader tr = new StreamReader(stream);
                models = (List<Model>)deserializer.Deserialize(tr);
            }
            else if (fileFormat == FileFormat.Binary)
            {*/
            BinaryFormatter bFormatter = new BinaryFormatter();
            try
            {
                models = (List<Model>)bFormatter.Deserialize(stream);
            }
            catch (Exception e)
            {
            }
            //}

            return models;
        }

    }
}
