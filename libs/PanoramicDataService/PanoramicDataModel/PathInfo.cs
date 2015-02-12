using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicDataModel
{

    public class PathInfo 
    {
        public PathInfo(TableInfo root)                     { Path = new List<TableDependency>(); TableInfo = root; }
        public PathInfo(IEnumerable<TableDependency> deps) { Path = new List<TableDependency>(deps); TableInfo = Path.Last().ToTableInfo; }
        public PathInfo(PathInfo path, TableDependency dep) { 
            Path = new List<TableDependency>(path.Path.ToArray());
            Path.Add(dep);
            TableInfo = Path.Last().ToTableInfo;
        }

        public string GetLabel()
        {
            string label = this.TableInfo.Name;
            if (this.TableInfo.TableAliases.Count > 0)
            {
                label = this.TableInfo.TableAliases.First().Alias;
            }

            if (this.Path.Count > 0 && this.Path.Last().RelationshipName != "")
            {
                label = this.Path.Last().RelationshipName;
            }
            return label;
        }

        public PathInfo LevelUp()
        {
            List<TableDependency> deps = Path.ToArray().ToList();
            TableInfo lastFromTableInfo = deps.Last().FromTableInfo;
            deps.RemoveAt(deps.Count - 1);

            PathInfo ret = null;
            if (deps.Count == 0)
            {
                ret = new PathInfo(lastFromTableInfo);
            }
            else
            {
                ret = new PathInfo(deps);
            }
            return ret;
        }

        public override int GetHashCode()
        {
            int code = TableInfo.GetHashCode();
            foreach (var p in Path)
                code ^= p.GetHashCode();
            return code;
        }
        public override bool Equals(object obj)
        {
            if (obj is PathInfo)
            {
                var path = obj as PathInfo;
                if (path.TableInfo.Id == TableInfo.Id)
                {
                    if (path.Path.Count == Path.Count)
                    {
                        for (int i = 0; i < path.Path.Count; i++)
                            if (path.Path[i].Id != Path[i].Id)
                                return false;
                        return true;
                    }
                }
            }
            return false;
        }

        public List<TableDependency> Path  { get; set; }
        public TableInfo             TableInfo { get; set; }
    }
}
