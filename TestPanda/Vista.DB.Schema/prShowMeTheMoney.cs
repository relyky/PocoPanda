namespace Vista.DB.Schema
{
using System;
using System.Collections.Generic;
using Dapper;
using Vista.DbPanda;
using Microsoft.Data.SqlClient;


static partial class DBHelperClassExtensions
{
public static int CallprShowMeTheMoney(this SqlConnection conn, SqlTransaction? txn = null)
{
  var result = conn.Execute("dbo.prShowMeTheMoney",
    transaction: txn,
    commandType: System.Data.CommandType.StoredProcedure
    );
  return result;
}
}
}

