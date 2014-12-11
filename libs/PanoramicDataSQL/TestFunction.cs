using System;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

public partial class UserDefinedFunctions
{
    public const double SALES_TAX = .086;

    [SqlFunction()]
    public static SqlDouble addTax(SqlDouble originalAmount)
    {
        SqlDouble taxAmount = originalAmount * SALES_TAX;

        return originalAmount + taxAmount;
    }
}
