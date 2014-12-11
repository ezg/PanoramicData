using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;
using System.Collections.Generic;
using System.IO;

[Serializable]
[Microsoft.SqlServer.Server.SqlUserDefinedAggregate(
    Format.UserDefined,
    IsInvariantToNulls = true,
    IsInvariantToDuplicates = false,
    IsInvariantToOrder = true,
    MaxByteSize = -1)] // allows to store lots of data (i.e. big size of IntermediateResult)
public struct group_concat : IBinarySerialize 
{
    public List<string> IntermediateResult;

    public void Init()
    {
        IntermediateResult = new List<string>();
    }

    public void Accumulate(SqlString Value)
    {
        IntermediateResult.Add(Value.ToString());
    }

    public void Merge (group_concat Group)
    {
        IntermediateResult.AddRange(Group.IntermediateResult);
    }

    [return: SqlFacet(MaxSize = -1)] // allows for big return strings
    public SqlString Terminate ()
    {
        IntermediateResult.Sort();
        return string.Join(", ", IntermediateResult.ToArray());
    }

    public void Write(BinaryWriter w)
    {
        w.Write(IntermediateResult.Count);
        foreach (string theString in IntermediateResult)
            w.Write(theString);
    }

    public void Read(BinaryReader r)
    {
        this.IntermediateResult = new List<string>();
        int numStrings = r.ReadInt32();

        for (int i = 0; i < numStrings; i++)
            IntermediateResult.Add(r.ReadString());
    }
}
