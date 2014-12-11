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
    IsInvariantToOrder = false,
    MaxByteSize = -1)] // allows to store lots of data (i.e. big size of IntermediateResult)
public struct group_concat_double : IBinarySerialize 
{
    public List<double> IntermediateResult;

    public void Init()
    {
        IntermediateResult = new List<double>();
    }

    public void Accumulate(SqlDouble value)
    {
        if (!value.IsNull)
        {
            IntermediateResult.Add(value.Value);
        }
    }

    public void Merge (group_concat_double Group)
    {
        IntermediateResult.AddRange(Group.IntermediateResult);
    }

    [return: SqlFacet(MaxSize = -1)] // allows for big return doubles
    public SqlString Terminate()
    {
        //IntermediateResult.Sort();
        StringBuilder sb = new StringBuilder();
        foreach (var d in IntermediateResult)
        {
            sb.Append(d + ",");
        }
        sb.Remove(sb.Length - 2, 2);
        return sb.ToString();
    }

    public void Write(BinaryWriter w)
    {
        w.Write(IntermediateResult.Count);

        foreach (double thedouble in IntermediateResult)
            w.Write(thedouble);
    }

    public void Read(BinaryReader r)
    {
        this.IntermediateResult = new List<double>();
        int numdoubles = r.ReadInt32();

        for (int i = 0; i < numdoubles; i++)
            IntermediateResult.Add(r.ReadDouble());
    }
}
