
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Graphviz4Net.Graphs;
using System.ComponentModel;

namespace KnowledgeSketchSchemaParser
{

    public class Table
    {
        private readonly Graph<Table> graph;

        public Table(Graph<Table> graph)
        {
            this.graph = graph;
        }

        public string TableName { get; set; }

        public string TableLabel { get; set; }

        public ICommand RemoveCommand
        {
            get { return new RemoveCommandImpl(this); }
        }

        private class RemoveCommandImpl : ICommand
        {
            private Table table;

            public RemoveCommandImpl(Table table)
            {
                this.table = table;
            }

            public void Execute(object parameter)
            {
                this.table.graph.RemoveVertexWithEdges(this.table);
            }

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;
        }
    }

    public class DiamondArrow
    {
    }

    public class Arrow
    {
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public MainWindowViewModel()
        {
            var graph = new Graph<Table>();
            var a = new Table(graph) { TableName = "Jonh", TableLabel = "./Avatars/avatar1.jpg" };
            var b = new Table(graph) { TableName = "Michael", TableLabel = "./Avatars/avatar2.gif" };
            var c = new Table(graph) { TableName = "Kenny" };
            var d = new Table(graph) { TableName = "Lisa" };
            var e = new Table(graph) { TableName = "Lucy", TableLabel = "./Avatars/avatar3.jpg" };
            var f = new Table(graph) { TableName = "Ted Mosby" };
            var g = new Table(graph) { TableName = "Glen" };
            var h = new Table(graph) { TableName = "Alice", TableLabel = "./Avatars/avatar1.jpg" };

            graph.AddVertex(a);
            graph.AddVertex(b);
            graph.AddVertex(c);
            graph.AddVertex(d);
            graph.AddVertex(e);
            graph.AddVertex(f);

            var subGraph = new SubGraph<Table> { Label = "Work" };
            graph.AddSubGraph(subGraph);
            subGraph.AddVertex(g);
            subGraph.AddVertex(h);
            graph.AddEdge(new Edge<Table>(g, h));
            graph.AddEdge(new Edge<Table>(a, g));

            var subGraph2 = new SubGraph<Table> { Label = "School" };
            graph.AddSubGraph(subGraph2);
            var loner = new Table(graph) { TableName = "Loner", TableLabel = "./Avatars/avatar1.jpg" };
            subGraph2.AddVertex(loner);
            graph.AddEdge(new Edge<SubGraph<Table>>(subGraph, subGraph2) { Label = "Link between groups" });

            graph.AddEdge(new Edge<Table>(c, d) { Label = "In love", DestinationArrowLabel = "boyfriend", SourceArrowLabel = "girlfriend" });

            graph.AddEdge(new Edge<Table>(c, g, new Arrow(), new Arrow()));
            graph.AddEdge(new Edge<Table>(c, a, new Arrow()) { Label = "Boss" });
            graph.AddEdge(new Edge<Table>(d, h, new DiamondArrow(), new DiamondArrow()));
            graph.AddEdge(new Edge<Table>(f, h, new DiamondArrow(), new DiamondArrow()));
            graph.AddEdge(new Edge<Table>(f, loner, new DiamondArrow(), new DiamondArrow()));
            graph.AddEdge(new Edge<Table>(f, b, new DiamondArrow(), new DiamondArrow()));
            graph.AddEdge(new Edge<Table>(e, g, new Arrow(), new Arrow()) { Label = "Siblings" });

            this.Graph = graph;
            this.Graph.Changed += GraphChanged;
            this.NewPersonName = "Enter new name";
        }

        public Graph<Table> Graph { get; private set; }

        public string NewPersonName { get; set; }

        public IEnumerable<string> PersonNames
        {
            get { return this.Graph.AllVertices.Select(x => x.TableName); }
        }

        public string NewEdgeStart { get; set; }

        public string NewEdgeEnd { get; set; }

        public string NewEdgeLabel { get; set; }

        public void CreateEdge()
        {
            if (string.IsNullOrWhiteSpace(this.NewEdgeStart) ||
                string.IsNullOrWhiteSpace(this.NewEdgeEnd))
            {
                return;
            }

            this.Graph.AddEdge(
                new Edge<Table>
                    (this.GetTable(this.NewEdgeStart),
                    this.GetTable(this.NewEdgeEnd))
                {
                    Label = this.NewEdgeLabel
                });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void GraphChanged(object sender, GraphChangedArgs e)
        {
            this.RaisePropertyChanged("PersonNames");
        }

        private void RaisePropertyChanged(string property)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }

        private Table GetTable(string name)
        {
            return this.Graph.AllVertices.First(x => string.CompareOrdinal(x.TableName, name) == 0);
        }
    }
}
