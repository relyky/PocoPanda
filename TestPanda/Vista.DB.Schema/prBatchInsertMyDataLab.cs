namespace Vista.DB.Schema
{
using System;
using System.Collections.Generic;
using Dapper;
using Vista.DbPanda;
using Microsoft.Data.SqlClient;


public class prBatchInsertMyDataLabArgs 
{
  public List<MyDataTvp>? dataList { get; set; }
  public string foo { get; set; } = default!;
}

static partial class DBHelperClassExtensions
{
public static int CallprBatchInsertMyDataLab(this SqlConnection conn, prBatchInsertMyDataLabArgs args, SqlTransaction? txn = null)
{
  var param = new DynamicParameters();
  if(args.dataList == null) throw new ApplicationException("args.dataList 不可為NULL。"); 
  param.Add("@dataList", args.dataList.AsDataTable().AsTableValuedParameter(nameof(MyDataTvp))); 
  param.Add("@foo", args.foo); 

  var result = conn.Execute("dbo.prBatchInsertMyDataLab", param,
    transaction: txn,
    commandType: System.Data.CommandType.StoredProcedure
    );
  return result;
}

public static int CallprBatchInsertMyDataLab(this SqlConnection conn, List<MyDataTvp>? dataList, string foo, SqlTransaction? txn = null)
{
  var args = new prBatchInsertMyDataLabArgs {
    dataList = dataList,
    foo = foo,
  };

  var result = conn.CallprBatchInsertMyDataLab(args, txn); 
  return result;
}
}
}

