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
public struct match_percentage : IBinarySerialize 
{
    public List<string> IntermediateResult;
    public string Needle;
    public string Predicate;

    public void Init()
    {
        IntermediateResult = new List<string>();
    }

    public void Accumulate(SqlString value, SqlString needle, SqlString predicate)
    {
        if (!value.IsNull)
        {
            IntermediateResult.Add(value.Value);
        }
        Needle = needle.Value;
        Predicate = predicate.Value;
    }

    public void Merge (match_percentage Group)
    {
        IntermediateResult.AddRange(Group.IntermediateResult);
    }

    [return: SqlFacet(MaxSize = -1)] // allows for big return strings
    public SqlDouble Terminate()
    {
        double matchCount = 0;
        double needleAsDouble = 0;
        bool parsed = double.TryParse(Needle, out needleAsDouble);
        if (!parsed && (Predicate == "GREATER_THAN" || Predicate == "LESS_THAN"))
        {
            return 0;
        }

        foreach (var d in IntermediateResult)
        {
            if (Predicate == "EQUALS")
            {
                if (d == Needle)
                    matchCount++;
            }
            else if (Predicate == "GREATER_THAN")
            {
                if (double.Parse(d) > needleAsDouble)
                    matchCount++;
            }
            else if (Predicate == "LESS_THAN")
            {
                if (double.Parse(d) < needleAsDouble)
                    matchCount++;
            }
            else if (Predicate == "LIKE")
            {
                if (d.Contains(Needle))
                    matchCount++;
            }
        }
        if (IntermediateResult.Count > 0)
        {
            return matchCount / (double)IntermediateResult.Count;
        }
        else
        {
            return 0.0;
        }
    }

    public void Write(BinaryWriter w)
    {
        w.Write(IntermediateResult.Count);
        
        foreach (string d in IntermediateResult)
            w.Write(d);
        
        w.Write(Needle);
        w.Write(Predicate);
    }

    public void Read(BinaryReader r)
    {
        this.IntermediateResult = new List<string>();
        int numDs = r.ReadInt32();

        for (int i = 0; i < numDs; i++)
            IntermediateResult.Add(r.ReadString());

        Needle = r.ReadString();
        Predicate = r.ReadString();
    }
}
