using Dapper;
using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using System.Text;

/***************************************************************************
第三版 DBHelper.v３.0 on 2023-1-18
參考相依：Vista.Models.GenericListDataReader 類別。
***************************************************************************/

namespace Vista.DbPanda;

public static class DBHelperClassExtensions
{
  /// <summary>
  /// DBHelper: cast as Dapper CommandDefinition
  /// </summary>
  public static CommandDefinition AsDapperCommand(this SqlCommand sql)
  {
    if (sql.Parameters.Count > 0)
    {
      DynamicParameters args = new DynamicParameters();
      foreach (SqlParameter p in sql.Parameters)
      {
        args.Add(p.ParameterName, p.Value);
      }

      return new CommandDefinition(sql.CommandText, args);
    }
    else
    {
      return new CommandDefinition(sql.CommandText);
    }
  }

  /// <summary>
  /// DBHelper: 取代 Dapper.Contrib 之 Get 指令無法多 p-key 取值的狀況
  /// </summary>
  public static TTable GetEx<TTable>(this SqlConnection conn, object keys, SqlTransaction txn = null)
  {
    // 依 Property 動態加入 P-Key 查詢條件
    List<String> conds = new List<string>();
    foreach (PropertyInfo pi in keys.GetType().GetProperties())
    {
      conds.Add($"{pi.Name} = @{pi.Name} ");
    }

    String tableName = typeof(TTable).Name;
    StringBuilder sql = new StringBuilder($@"SELECT TOP 1 * FROM {tableName} WHERE {String.Join("AND ", conds)}; ");
    var info = conn.Query<TTable>(sql.ToString(), keys, txn).FirstOrDefault();
    return info;
  }

  /// <summary>
  /// DBHelper: 載入多筆資料。設計用於取主檔下的多筆明細資料。與GetEx相比可以取回多筆資料。
  /// </summary>
  public static List<TTable> LoadEx<TTable>(this SqlConnection conn, object keys, SqlTransaction txn = null)
  {
    // 依 Property 動態加入 P-Key 查詢條件
    List<String> conds = new List<string>();
    if (keys != null)
    {
      foreach (PropertyInfo pi in keys.GetType().GetProperties())
      {
        conds.Add($"{pi.Name} = @{pi.Name} ");
      }
    }

    String tableName = typeof(TTable).Name;
    StringBuilder sql = new StringBuilder($@"SELECT * FROM {tableName} {(conds.Count > 0 ? "WHERE" : "")} {String.Join("AND ", conds)}; ");
    var dataList = conn.Query<TTable>(sql.ToString(), keys, txn).ToList();
    return dataList;
  }

  /// <summary>
  /// DBHelper: 可刪除多筆資料。
  /// </summary>
  public static int DeleteEx<TTable>(this SqlConnection conn, object keys, SqlTransaction txn = null)
  {
    // 依 Property 動態加入 P-Key 查詢條件
    List<String> conds = new List<string>();
    foreach (PropertyInfo pi in keys.GetType().GetProperties())
    {
      conds.Add($"{pi.Name} = @{pi.Name} ");
    }

    String tableName = typeof(TTable).Name;
    StringBuilder sql = new StringBuilder($@"DELETE FROM {tableName} WHERE {String.Join("AND ", conds)}; ");
    int ret = conn.Execute(sql.ToString(), keys, txn);
    return ret;
  }

  /// <summary>
  /// DBHelper: UPDATE TABLE。為了一種經常性的應用：標記已刪除等。或只更一筆資料中的幾個（二、三個）欄位。
  /// ※注意：不可用於更新P-Key的值。
  /// </summary>
  /// <param name="newValues">新的值，請用Anonymous Type。</param>
  /// <param name="keys">P-Keys，請用Anonymous Type。</param>
  /// <returns>updCount，更新筆數。</returns>
  public static int UpdateEx<TTable>(this SqlConnection conn, object newValues, object keys, SqlTransaction txn = null)
  {
    DynamicParameters param = new DynamicParameters();
    var tableType = typeof(TTable);
    var tableProps = tableType.GetProperties();

    //## 排除 identity 與 Computed field。
    /// 它們都是 DatabaseGeneratedAttribute。
    var skipProperties = tableProps.Where(p => p.GetCustomAttributes(true).Any(a => a is DatabaseGeneratedAttribute)).ToArray();

    // 依 Property 動態加入更新欄位
    List<String> fields = new List<string>();
    foreach (PropertyInfo pi in newValues.GetType().GetProperties())
    {
      //# 排除更新欄位
      if (skipProperties.Length > 0 && skipProperties.Any(c => String.Compare(c.Name, pi.Name, true) == 0)) continue;

      //# 加入更新欄位
      fields.Add($"{pi.Name} = @{pi.Name} ");
      param.Add(pi.Name, pi.GetValue(newValues));
    }

    // 依 Property 動態加入 P-Key 查詢條件
    List<String> conds = new List<string>();
    foreach (PropertyInfo pi in keys.GetType().GetProperties())
    {
      conds.Add($"{pi.Name} = @{pi.Name} ");
      param.Add(pi.Name, pi.GetValue(keys));
    }

    String tableName = typeof(TTable).Name;
    StringBuilder sql = new StringBuilder($@"UPDATE {tableName} SET {String.Join(", ", fields)} WHERE {String.Join("AND ", conds)}; ");
    int updCount = conn.Execute(sql.ToString(), param, txn);
    return updCount;
  }

  /// <summary>
  /// DBHelper: Insert multiple entities into table "TTable".
  /// </summary>
  public static int InsertEx<TTable>(this SqlConnection conn, IEnumerable<TTable> entityList, SqlTransaction txn = null)
  {
    var tableType = typeof(TTable);
    var tableProps = tableType.GetProperties();

    //## 排除 identity 與 Computed field。
    /// 它們都是 DatabaseGeneratedAttribute。
    var skipProperties = tableProps.Where(p => p.GetCustomAttributes(true).Any(a => a is DatabaseGeneratedAttribute)).ToArray();
    if (skipProperties.Length > 0)
      tableProps = tableProps.Except(skipProperties).ToArray();

    string insertCmd = $"INSERT INTO {tableType.Name}" +
      $"({String.Join(",", tableProps.Select(pi => pi.Name))}) " +
      $"VALUES " +
      $"({String.Join(",", tableProps.Select(pi => "@" + pi.Name))}); ";

    return conn.Execute(insertCmd, entityList, txn);
  }

  /// <summary>
  /// DBHelper: Insert one entity into table "TTable" and returns identity id.
  /// 參考自：[Dapper.Contrib\Insert<T>指令](https://github.com/DapperLib/Dapper.Contrib/blob/main/src/Dapper.Contrib/SqlMapperExtensions.cs)
  /// </summary>
  public static long InsertEx<TTable>(this SqlConnection conn, TTable entity, SqlTransaction txn = null)
  {
    var tableType = typeof(TTable);
    var tableProps = tableType.GetProperties();

    //## 排除 identity 與 Computed field。
    /// 它們都是 DatabaseGeneratedAttribute。
    PropertyInfo[] skipProperties = tableProps.Where(p => p.GetCustomAttributes(true).Any(attr => attr is DatabaseGeneratedAttribute)).ToArray();
    if (skipProperties.Length > 0)
      tableProps = tableProps.Except(skipProperties).ToArray();

    //## 取出 identity field
    PropertyInfo? identityKey = skipProperties.FirstOrDefault(p => p.GetCustomAttributes(true)
      .Any(attr => attr is DatabaseGeneratedAttribute && ((DatabaseGeneratedAttribute)attr).DatabaseGeneratedOption == DatabaseGeneratedOption.Identity));

    string insertCmd = $"INSERT INTO {tableType.Name}" +
      $"({String.Join(",", tableProps.Select(pi => pi.Name))}) " +
      $"VALUES " +
      $"({String.Join(",", tableProps.Select(pi => "@" + pi.Name))}); " +
      $"SELECT ISNULL(SCOPE_IDENTITY(),0) AS id"; // 並取回 identity 若有的話。

    long id = conn.ExecuteScalar<long>(insertCmd, entity, txn);

    //# 若有 identity 則填回 entity
    if (id > 0L && identityKey != null)
      identityKey.SetValue(entity, Convert.ChangeType(id, identityKey.PropertyType), null);

    // 
    return id;
  }

  /// <summary>
  /// BulkInsert/BulkCopy
  /// ref → https://riptutorial.com/dapper/example/21711/bulk-copy
  /// </summary>
  public static void BulkInsert<TTable>(this SqlConnection conn, IEnumerable<TTable> entityList, string? tableName = null, int bulkCopyTimeout = 30, int batchSize = 500)
  {
    var tableType = typeof(TTable);

    using (IDataReader reader = entityList.GetDataReader())
    using (var bulkCopy = new SqlBulkCopy(conn))
    {
      bulkCopy.BulkCopyTimeout = bulkCopyTimeout;
      bulkCopy.BatchSize = batchSize;
      bulkCopy.DestinationTableName = tableName ?? tableType.Name;
      bulkCopy.EnableStreaming = true;
      using (var dataReader = reader)
      {
        bulkCopy.WriteToServer(dataReader);
      }
    }
  }

  public static DataTable QueryAsDataTable(this SqlConnection conn, string sql, object param = null, IDbTransaction txn = null, int? commandTimeout = null, CommandType? commandType = null)
  {
    var dt = new DataTable();
    using IDataReader reader = conn.ExecuteReader(sql, param, txn, commandTimeout, commandType);
    dt.Load(reader);
    return dt;
  }

  public static List<TTable> QueryEx<TTable>(this SqlConnection conn, string sql, List<SqlParameter>? paramList = null, SqlTransaction? txn = null)
  where TTable : class
  {
    SqlCommand cmd = new SqlCommand(sql, conn, txn);

    // 加入查詢條件
    if (paramList != null)
      cmd.Parameters.AddRange(paramList.ToArray());

    // 執行 SqlCommnad
    using var reader = cmd.ExecuteReader();

    List<TTable> dataList = new();
    while (reader.Read())
    {
      dataList.Add(reader.MapToObject<TTable>());
    }

    return dataList;
  }

  public static List<TTable> QueryEx<TTable>(this SqlCommand cmd)
    where TTable : class
  {
    // 執行 SqlCommnad
    using var reader = cmd.ExecuteReader();

    List<TTable> dataList = new();
    while (reader.Read())
    {
      dataList.Add(reader.MapToObject<TTable>());
    }

    return dataList;
  }


  #region Helper Funcitn

  /// <summary>
  /// 轉換 List<T> 成 DataTable。
  /// for TVP(table value parameter) 參數傳遞
  /// </summary>
  public static DataTable AsDataTable<TTableType>(this List<TTableType> infoList)
  {
    var table = new DataTable();
    var properties = typeof(TTableType).GetRuntimeProperties();

    foreach (var prop in properties)
    {
      table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
    }

    foreach (var info in infoList)
    {
      table.Rows.Add(properties.Select(property => property.GetValue(info)).ToArray());
    }

    return table;
  }

  #endregion
}