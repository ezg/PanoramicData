using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace MongoDBTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void createData(MongoCollection<BsonDocument> collection)
        {
            Random r = new Random();
            for (int i = 0; i < 50000; i++)
            {
                string type = "";
                double tt = r.NextDouble();
                if (tt > 0.5)
                {
                    type = "A";
                }
                else
                {
                    type = "B";
                }


                BsonDocument book = new BsonDocument {
                    { "name", "name_" + i },
                    { "stat1", r.NextDouble() },
                    { "stat2", (int) (r.NextDouble() * 100)},
                    { "type", type }
                };
                collection.Insert(book);
            }
            return;
        }

        private void aggregate(MongoCollection<BsonDocument> collection)
        {
            var agg = collection.Aggregate(
                new BsonDocument
                {
                    {
                        "$group",
                        new BsonDocument()
                        {
                            {"_id", "$type"},
                            {
                                "total",
                                new BsonDocument
                                {
                                    {"$avg", "$stat1"}
                                }
                            },
                            {
                                "test",
                                new BsonDocument
                                {
                                    {"$avg", "$stat2"}
                                }
                            }
                        }
                    }
                },
                new BsonDocument
                {
                    {
                        "$sort",
                        new BsonDocument
                        {
                            {"total", -1}
                        }
                    }
                },
                new BsonDocument
                {
                    {
                        "$project",
                         new BsonDocument()
                         {
                            {
                                "concat_Test",
                                new BsonDocument
                                {
                                    {"$concat", "$_id"}
                                }
                            }
                        }
                    }
                }
                );
            foreach (var resultDocument in agg.ResultDocuments)
            {
                Console.WriteLine(resultDocument);
            }
        }

        private void mapReduce(MongoCollection<BsonDocument> collection)
        {
            string map = @"
                function() {
                    var test = this;
                    emit(test.type, { count: 1, totalStats: test.stat1 });
                }";

            string reduce = @"        
                function(key, values) {
                    var result = {count: 0, totalStats: 0 };

                    values.forEach(function(value){               
                        result.count += value.count;
                        result.totalStats += value.totalStats;
                    });

                    return result;
                }";

            string finalize = @"
                function(key, value){
      
                  value.average = value.totalStats / value.count;
                  return value;

                }";

            var options = new MapReduceOptionsBuilder();
            options.SetFinalize(finalize);
            options.SetOutput(MapReduceOutput.Inline);
            var results = collection.MapReduce(map, reduce, options);

            foreach (var result in results.GetResults())
            {
                Console.WriteLine(result);
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            MongoClient client = new MongoClient(); // connect to localhost
            MongoServer server = client.GetServer();
            MongoDatabase db = server.GetDatabase("test");
            var testCollection = db.GetCollection("test");
            
            //createData(testCollection);
            //aggregate(testCollection);
            mapReduce(testCollection);
        }
    }
}
