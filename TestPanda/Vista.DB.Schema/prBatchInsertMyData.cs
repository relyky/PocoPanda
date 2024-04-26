namespace Vista.DB.Schema
{
using System;
using System.Collections.Generic;
using Dapper;
using Vista.DbPanda;
using Microsoft.Data.SqlClient;

public class prBatchInsertMyDataResult 
{
  public int RowsAffected { get; set; }
}

public class prBatchInsertMyDataArgs 
{
  public List<MyDataTvp>? dataList { get; set; }
}

static partial class DBHelperClassExtensions
{
public static List<prBatchInsertMyDataResult> CallprBatchInsertMyData(this SqlConnection conn, prBatchInsertMyDataArgs args, SqlTransaction? txn = null)
{
  var param = new DynamicParameters();
  if(args.dataList == null) throw new ApplicationException("args.dataList 不可為NULL。"); 
  param.Add("@dataList", args.dataList.AsDataTable().AsTableValuedParameter(nameof(MyDataTvp))); 

  var result = conn.Query<prBatchInsertMyDataResult>("dbo.prBatchInsertMyData", param,
    transaction: txn,
    commandType: System.Data.CommandType.StoredProcedure
    ).AsList();
  return result;
}

public static List<prBatchInsertMyDataResult> CallprBatchInsertMyData(this SqlConnection conn, List<MyDataTvp>? dataList, SqlTransaction? txn = null)
{
  var args = new prBatchInsertMyDataArgs {
    dataList = dataList,
  };

  var result = conn.CallprBatchInsertMyData(args, txn); 
  return result;
}
}
}

