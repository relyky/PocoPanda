namespace Vista.DB.Schema
{
using System;
using System.Collections.Generic;
using Dapper;
using Microsoft.Data.SqlClient;

public class vwMyDataResult 
{
  public Int64 SN { get; set; }
  public string IDN { get; set; } = default!;
  public string Title { get; set; } = default!;
  public Decimal Amount { get; set; }
  public DateTime? Birthday { get; set; }
  public TimeSpan? WakeTime { get; set; }
  public string Remark { get; set; } = default!;
  public DateTime? LogDtm { get; set; }
}

public class vwMyDataArgs 
{
  public string Title { get; set; } = default!;
  public Decimal? Amount { get; set; }
}

static partial class DBHelperClassExtensions
{
public static List<vwMyDataResult> CallvwMyData(this SqlConnection conn, vwMyDataArgs args, SqlTransaction? txn = null)
{
  var sql = @"SELECT * FROM [dbo].[vwMyData](@Title,@Amount); "; 
  var dataList = conn.Query<vwMyDataResult>(sql, args, txn).AsList();
  return dataList;
}

public static List<vwMyDataResult> CallvwMyData(this SqlConnection conn, string Title, Decimal? Amount, SqlTransaction? txn = null)
{
  var args = new {
    Title,
    Amount,
  };

  var sql = @"SELECT * FROM [dbo].[vwMyData](@Title,@Amount); "; 
  var dataList = conn.Query<vwMyDataResult>(sql, args, txn).AsList();
  return dataList;
}
}
}

