namespace Vista.DB.Schema
{
using System;
using System.Collections.Generic;
using Dapper;
using Vista.DbPanda;
using Microsoft.Data.SqlClient;

public class prQry_EXAM_APPResult 
{
  public string APP_NO { get; set; } = default!;
  public string APP_NAME { get; set; } = default!;
  public DateTime EFF_DATE { get; set; }
  public string OFF_DATE { get; set; } = default!;
  public string OFF_FLAG { get; set; } = default!;
  public string SALE_NO { get; set; } = default!;
  public string COMPANY { get; set; } = default!;
  public Decimal UNIT_PRICE { get; set; }
  public string PAY_TYPE { get; set; } = default!;
}

static partial class DBHelperClassExtensions
{
public static List<prQry_EXAM_APPResult> CallprQry_EXAM_APP(this SqlConnection conn, SqlTransaction? txn = null)
{
  var result = conn.Query<prQry_EXAM_APPResult>("dbo.prQry_EXAM_APP",
    transaction: txn,
    commandType: System.Data.CommandType.StoredProcedure
    ).AsList();
  return result;
}
}
}

