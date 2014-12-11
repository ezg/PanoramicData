using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.IO;
using System.Text;

[Serializable]
[Microsoft.SqlServer.Server.SqlUserDefinedAggregate(
    Format.UserDefined,
    IsInvariantToNulls = true,
    IsInvariantToDuplicates = false,
    IsInvariantToOrder = true,
    MaxByteSize = -1)] // allows to store lots of data (i.e. big size of IntermediateResult)
public struct xml_vis : IBinarySerialize 
{
    public List<double> IntermediateResult;
    public double Max;
    public double Min;

    public void Init()
    {
        IntermediateResult = new List<double>();
    }

    public void Accumulate(SqlDouble value, SqlDouble min, SqlDouble max)
    {
        if (!value.IsNull)
        {
            IntermediateResult.Add(value.Value);
        }
        Max = max.Value;
        Min = min.Value;
    }

    public void Merge (xml_vis Group)
    {
        IntermediateResult.AddRange(Group.IntermediateResult);
    }

    [return: SqlFacet(MaxSize = -1)] // allows for big return strings
    public SqlString Terminate ()
    {
        IntermediateResult.Sort();
        StringBuilder sb = new StringBuilder();
        sb.Append(Min + "," + Max + ",");
        foreach (var d in IntermediateResult)
        {
            sb.Append(d + ",");
        }
        return sb.ToString();
    }

    public void Write(BinaryWriter w)
    {
        w.Write(IntermediateResult.Count);
        foreach (double d in IntermediateResult)
            w.Write(d);
        w.Write(Max);
        w.Write(Min);
    }

    public void Read(BinaryReader r)
    {
        this.IntermediateResult = new List<double>();
        int numDs = r.ReadInt32();

        for (int i = 0; i < numDs; i++)
            IntermediateResult.Add(r.ReadDouble());

        Max = r.ReadDouble();
        Min = r.ReadDouble();
    }
}
