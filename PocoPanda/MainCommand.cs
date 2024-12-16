using ClosedXML.Excel;
using ClosedXML.Report;
using Cocona;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using PocoPanda.Models;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace PocoPanda;

/// <summary>
/// 主要命令
/// </summary>
class MainCommand
{
  //# 參數
  readonly string _connStr;
  readonly string _nameSpace;
  readonly string _outputFolder;
  readonly bool _exportExcel;
  readonly string _indent = "  ";

  /// <summary>
  /// "Microsoft.Data.SqlClient" OR "System.Data.SqlClient"
  /// </summary>
  readonly string _sqlClientLibrary;

  /// <summary>
  /// 把 Talbe 與 View 的 class name 串上'Info' 以避免衝突欄位名稱。
  /// </summary>
  readonly bool _nameInfo;

  /// <summary>
  /// 欄位定義加入 '= default!'。預設:true. 
  /// </summary>
  readonly bool _fieldWithDefault;

  /// <summary>
  /// .NET Framework 4.8 只支援到 C# 7.3 版的語言限制。
  /// </summary>
  readonly bool _cs73Limit;

  [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<T>(String, T)")]
  public MainCommand(IConfiguration config)
  {
    //# 取得參數
    _connStr = config.GetConnectionString("DefaultConnection");
    _nameSpace = config["Namespace"];
    _outputFolder = config["OutputFolder"];
    _exportExcel = config.GetValue<bool>("ExportExcel", false);
    _sqlClientLibrary = config.GetValue<string>("SqlClientLibrary", "Microsoft.Data.SqlClient");
    _nameInfo = config.GetValue<bool>("NameInfo", false);
    _fieldWithDefault = config.GetValue<bool>("FieldWithDefault", true);
    _cs73Limit = config.GetValue<bool>("CS73Limit", false);
  }

  [Command(Description = "SQL Server POCO tool。所有參數均填入 appsettings.json。")]
  public async Task CommandProcedure()
  {
    Console.WriteLine($"#BEGIN {nameof(MainCommand)}");
    Console.WriteLine($"§ 參數");
    Console.WriteLine($"連線字串：{_connStr}");
    Console.WriteLine($"輸出目錄：{_outputFolder}");
    Console.WriteLine($"命名空間：{_nameSpace}");
    Console.WriteLine($"SqlClient 套件：{_sqlClientLibrary}");
    Console.WriteLine($"產生 Excel：{_exportExcel}");
    Console.WriteLine($"類別名稱串 Info：{_nameInfo}");
    Console.WriteLine($"欄位宣告有 defautl：{_fieldWithDefault}");
    Console.WriteLine($"C# 7.3 限制：{_cs73Limit}");
    Console.WriteLine();

    try
    {
      //# 建立輸出目錄
      DirectoryInfo outDir = new DirectoryInfo(_outputFolder);
      if (!outDir.Exists) outDir.Create();

      //# 開始產生 POCO classes 
      using var conn = new SqlConnection(_connStr);
      await conn.OpenAsync();

      GenerateTablePocoCode(conn, outDir);
      GenerateProcPocoCode(conn, outDir);
      GenerateTableValuedFunctionPocoCode(conn, outDir);
      GenerateTableTypePocoCode(conn, outDir);

      if (_exportExcel) 
        GenerateTableToExcel(conn, outDir);

      Console.WriteLine();
      Console.WriteLine("已成功產生 Dapper POCO 程式碼，請檢查輸出目錄。");
    }
    finally
    {
#if !DEBUG
      Console.WriteLine("Press any key to continue.");
      Console.ReadKey();
#endif
    }
  }

  /// <summary>
  /// Table & View
  /// </summary>
  void GenerateTablePocoCode(SqlConnection conn, DirectoryInfo outDir)
  {
    Console.WriteLine("================================================================================");
    Console.WriteLine($"#BEGIN {nameof(GenerateTablePocoCode)}");
    var tableList = DBHelper.LoadTable(conn); 
    tableList.ForEach(table =>
    {
      Console.WriteLine("--------------------------------------------------------------------------------");
      Console.WriteLine($"#{table.TABLE_TYPE}: {table.TABLE_CATALOG}.{table.TABLE_SCHEMA}.{table.TABLE_NAME}");
      StringBuilder pocoCode = new StringBuilder();

      pocoCode.AppendLine($"namespace {_nameSpace}");
      pocoCode.AppendLine("{");
      pocoCode.AppendLine("using System;");
      pocoCode.AppendLine("using System.ComponentModel.DataAnnotations;");
      pocoCode.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
      pocoCode.AppendLine();

      //# summary 
      if(table.MS_Description != null)
      {
        pocoCode.AppendLine($"/// <summary>");
        pocoCode.AppendLine($"/// {table.MS_Description}");
        pocoCode.AppendLine($"/// </summary>");
      }

      string tableClassName = _nameInfo
        ? $"{table.TABLE_NAME}Info"
        : table.TABLE_NAME;

      pocoCode.AppendLine($"[Table(\"{table.TABLE_NAME}\")]");
      pocoCode.AppendLine($"public class {tableClassName} ");
      pocoCode.AppendLine("{");
      //---------------------
      var columnList = DBHelper.LoadTableColumn(conn, table.TABLE_NAME, table.TABLE_SCHEMA);
      columnList.ForEach(col =>
      {
        string dataType = DBHelper.MapNetDataType(col.DATA_TYPE);
        bool isPrimaryKey = col.IS_PK == "YES";
        bool isIdentity = col.IS_IDENTITY == "YES";
        bool isComputed = col.IS_COMPUTED == "YES";
        string nullable = DetermineNullable("YES", dataType, isPrimaryKey); // (dataType != "string" && !isPrimaryKey) ? "?" : string.Empty; // ORM 欄位原則上都是 nullable 不然在 input binding 會很難實作。
        string description = col.MS_Description ?? string.Empty;

        //# summary
        if (col.MS_Description != null || col.COMPUTED_DEFINITION != null)
        {
          pocoCode.AppendLine($"{_indent}/// <summary>");
          if (col.MS_Description != null)
            pocoCode.AppendLine($"{_indent}/// {col.MS_Description}");
          if (col.COMPUTED_DEFINITION != null)
            pocoCode.AppendLine($"{_indent}/// Computed Definition: {col.COMPUTED_DEFINITION}");
          pocoCode.AppendLine($"{_indent}/// </summary>");
        }

        //# Display(Name) := 欄位名稱:欄位說明欄位說明欄位說明欄位說明。
        if (!String.IsNullOrWhiteSpace(col.MS_Description))
        {
          string displayName = col.MS_Description.Split(':', '：', '\r', '\n')[0].Trim();
          pocoCode.AppendLine($"{_indent}[Display(Name = \"{displayName}\")]");
        }

        //# Key & Computed attribute
        if (isPrimaryKey)
          pocoCode.AppendLine($"{_indent}[Key]");
        if (isIdentity)
          pocoCode.AppendLine($"{_indent}[DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
        if (isComputed)
          pocoCode.AppendLine($"{_indent}[DatabaseGenerated(DatabaseGeneratedOption.Computed)]");

        //# Required attribute
        if (col.IS_NULLABLE == "NO")
          pocoCode.AppendLine($"{_indent}[Required]");

        //# filed
        string? defaultString = (dataType == "string" && _fieldWithDefault) ? " = default!;" : null;
        pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {col.COLUMN_NAME} {{ get; set; }}{defaultString}");
      });

      //## 產生Copy函式
      ///public void Copy(職員基本表 src)
      ///{
      ///    this.職員代碼 = src.職員代碼;
      ///    this.職員姓名 = src.職員姓名;
      ///    this.狀態 = src.狀態;
      ///}

      pocoCode.AppendLine();
      pocoCode.AppendLine($"{_indent}public void Copy({tableClassName} src)");
      pocoCode.AppendLine($"{_indent}{{");
      columnList.ForEach(col =>
                pocoCode.AppendLine($"{_indent}{_indent}this.{col.COLUMN_NAME} = src.{col.COLUMN_NAME};"));
      pocoCode.AppendLine($"{_indent}}}"); // end of: Copy

      //## 產生Clone函式
      ///public 職員基本表 Clone()
      ///{
      ///    return new 職員基本表
      ///    {
      ///        職員代碼 = this.職員代碼,
      ///        職員姓名 = this.職員姓名,
      ///        狀態 = this.狀態
      ///    };
      ///}

      pocoCode.AppendLine();
      pocoCode.AppendLine($"{_indent}public {tableClassName} Clone()");
      pocoCode.AppendLine($"{_indent}{{");
      pocoCode.AppendLine($"{_indent}{_indent}return new {tableClassName} {{");
      columnList.ForEach(col =>
                pocoCode.AppendLine($"{_indent}{_indent}{_indent}{col.COLUMN_NAME} = this.{col.COLUMN_NAME},"));
      pocoCode.AppendLine($"{_indent}{_indent}}};");
      pocoCode.AppendLine($"{_indent}}}"); // end of: Clone

      //---------------------
      pocoCode.AppendLine("}"); // end of: Class
      pocoCode.AppendLine("}"); // end of: Namespace
      pocoCode.AppendLine();

      //## 一個 Table 一個檔案
      File.WriteAllText(Path.Combine(outDir.FullName, $"{table.TABLE_NAME}.cs"), pocoCode.ToString(), encoding: Encoding.UTF8);
      Console.WriteLine(pocoCode.ToString());
    });
  }

  /// <summary>
  /// 叫用 Procedure。有支援TVP。
  /// </summary>
  void GenerateProcPocoCode(SqlConnection conn, DirectoryInfo outDir)
  {
    Console.WriteLine("================================================================================");
    Console.WriteLine($"#BEGIN {nameof(GenerateProcPocoCode)}");
    var procList = DBHelper.LoadProcedure(conn);
    procList.ForEach(proc =>
    {
      Console.WriteLine("--------------------------------------------------------------------------------");
      Console.WriteLine($"#{proc.ROUTINE_TYPE}: {proc.SPECIFIC_CATALOG}.{proc.SPECIFIC_SCHEMA}.{proc.SPECIFIC_NAME}");
      StringBuilder pocoCode = new StringBuilder();

      pocoCode.AppendLine($"namespace {_nameSpace}");
      pocoCode.AppendLine("{");
      pocoCode.AppendLine("using System;");
      pocoCode.AppendLine("using System.Collections.Generic;");
      pocoCode.AppendLine("using Dapper;");
      pocoCode.AppendLine("using Vista.DbPanda;");
      pocoCode.AppendLine($"using {_sqlClientLibrary};");
      pocoCode.AppendLine();

      //## Procedure Result Class ------------
      ///public class 計算資產編號Result
      ///{
      ///    public string 資產編號 { get; set; }
      ///}
      bool f_NonResult = proc.ColumnList.Count <= 0; // 旗標：無輸出資料欄位
      if (!f_NonResult)
      {
        pocoCode.AppendLine($"public class {proc.SPECIFIC_NAME}Result ");
        pocoCode.AppendLine("{");
        proc.ColumnList.ForEach(col =>
        {
          string dataType = DBHelper.MapNetDataType(col.DATA_TYPE);
          string nullable = DetermineNullable(col.IS_NULLABLE, dataType, false); // (dataType != "string" && col.IS_NULLABLE == "YES") ? "?" : "";
          string? defaultString = (dataType == "string" && _fieldWithDefault) ? " = default!;" : null;

          pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {col.COLUMN_NAME} {{ get; set; }}{defaultString}");
        });
        pocoCode.AppendLine("}"); // end of: Reslt Column 
      }

      //# Procedure Parameter Class ------------
      ///public class 計算資產編號Args
      ///{
      ///    public string 品項類別 { get; set; }
      ///}
      bool f_NonParam = proc.ParamList.Count <= 0; // 旗標：無輸入參數
      if (!f_NonParam)
      {
        pocoCode.AppendLine();
        pocoCode.AppendLine($"public class {proc.SPECIFIC_NAME}Args ");
        pocoCode.AppendLine("{");
        proc.ParamList.ForEach(arg =>
        {
          string dataType = arg.IS_TABLE_TYPE == "YES"
            ? $"List<{arg.DATA_TYPE}>"
            : DBHelper.MapNetDataType(arg.DATA_TYPE);

          string nullable = DetermineNullable("YES", dataType, false); // (dataType != "string") ? "?" : "";
          string? defaultString = (dataType == "string" && _fieldWithDefault) ? " = default!;" : null;
          pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {arg.PARAMETER_NAME.Substring(1)} {{ get; set; }}{defaultString}");
        });
        pocoCode.AppendLine("}"); // end of: Reslt Column 
      }

      //## Procedure Instance ------------
      ///static partial class DBHelperClassExtensions
      ///{
      ///    public static List<計算資產編號Result> Call計算資產編號(this SqlConnection conn, 計算資產編號Args args)
      ///    {
      ///        var result = conn.Query<計算資產編號Result>("計算資產編號", args,
      ///            commandType: System.Data.CommandType.StoredProcedure).AsList();
      ///        return result;
      ///    }
      ///}

      pocoCode.AppendLine();
      pocoCode.AppendLine("static partial class DBHelperClassExtensions");
      pocoCode.AppendLine("{");

      if (f_NonParam)
      {
        if (f_NonResult) // 無參數無結果
          pocoCode.AppendLine($"public static int Call{proc.SPECIFIC_NAME}(this SqlConnection conn, SqlTransaction? txn = null)");
        else // 無參數無結果
          pocoCode.AppendLine($"public static List<{proc.SPECIFIC_NAME}Result> Call{proc.SPECIFIC_NAME}(this SqlConnection conn, SqlTransaction? txn = null)");
      }
      else
      {
        if (f_NonResult) // 有參數無結果
          pocoCode.AppendLine($"public static int Call{proc.SPECIFIC_NAME}(this SqlConnection conn, {proc.SPECIFIC_NAME}Args args, SqlTransaction? txn = null)");
        else // 有參數有結果
          pocoCode.AppendLine($"public static List<{proc.SPECIFIC_NAME}Result> Call{proc.SPECIFIC_NAME}(this SqlConnection conn, {proc.SPECIFIC_NAME}Args args, SqlTransaction? txn = null)");
      }

      pocoCode.AppendLine("{"); // begin of: call procedure 

      if (f_NonParam)
      {
        if (f_NonResult)
        {
          pocoCode.AppendLine($"{_indent}var result = conn.Execute(\"{proc.SPECIFIC_SCHEMA}.{proc.SPECIFIC_NAME}\",");
          pocoCode.AppendLine($"{_indent}{_indent}transaction: txn,");
          pocoCode.AppendLine($"{_indent}{_indent}commandType: System.Data.CommandType.StoredProcedure");
          pocoCode.AppendLine($"{_indent}{_indent});");
        }
        else
        {
          pocoCode.AppendLine($"{_indent}var result = conn.Query<{proc.SPECIFIC_NAME}Result>(\"{proc.SPECIFIC_SCHEMA}.{proc.SPECIFIC_NAME}\",");
          pocoCode.AppendLine($"{_indent}{_indent}transaction: txn,");
          pocoCode.AppendLine($"{_indent}{_indent}commandType: System.Data.CommandType.StoredProcedure");
          pocoCode.AppendLine($"{_indent}{_indent}).AsList();");
        }
      }
      else // 有參數，用 DynamicParameters 送入。
      {
        //# 重組輸入參數 args => DynamicParameters
        /// var param = new DynamicParameters();
        /// if(args.dataList == null) throw new ApplicaationException("args.dataList 不可為NULL。");
        /// param.Add("@dataList", args.dataList.AsDataTable().AsTableValuedParameter(nameof(MyDataTvp));
        /// param.Add("@foo", args.foo);
        pocoCode.AppendLine($"{_indent}var param = new DynamicParameters();");
        proc.ParamList.ForEach(arg =>
        {
          if (arg.IS_TABLE_TYPE == "YES")
          {
            pocoCode.AppendLine($"{_indent}if(args.{arg.PARAMETER_NAME.Substring(1)} == null) throw new ApplicationException(\"args.{arg.PARAMETER_NAME.Substring(1)} 不可為NULL。\"); ");
            pocoCode.AppendLine($"{_indent}param.Add(\"{arg.PARAMETER_NAME}\", args.{arg.PARAMETER_NAME.Substring(1)}.AsDataTable().AsTableValuedParameter(nameof({arg.DATA_TYPE}))); ");
          }
          else
            pocoCode.AppendLine($"{_indent}param.Add(\"{arg.PARAMETER_NAME}\", args.{arg.PARAMETER_NAME.Substring(1)}); ");
        });
        pocoCode.AppendLine();

        if (f_NonResult)
        {
          pocoCode.AppendLine($"{_indent}var result = conn.Execute(\"{proc.SPECIFIC_SCHEMA}.{proc.SPECIFIC_NAME}\", param,");
          pocoCode.AppendLine($"{_indent}{_indent}transaction: txn,");
          pocoCode.AppendLine($"{_indent}{_indent}commandType: System.Data.CommandType.StoredProcedure");
          pocoCode.AppendLine($"{_indent}{_indent});");
        }
        else
        {
          pocoCode.AppendLine($"{_indent}var result = conn.Query<{proc.SPECIFIC_NAME}Result>(\"{proc.SPECIFIC_SCHEMA}.{proc.SPECIFIC_NAME}\", param,");
          pocoCode.AppendLine($"{_indent}{_indent}transaction: txn,");
          pocoCode.AppendLine($"{_indent}{_indent}commandType: System.Data.CommandType.StoredProcedure");
          pocoCode.AppendLine($"{_indent}{_indent}).AsList();");
        }
      }

      pocoCode.AppendLine($"{_indent}return result;");
      pocoCode.AppendLine("}"); // end of: call procedure

      //## Procedure Instance : method 2 直接帶入參數 ------------
      ///static partial class DBHelperClassExtensions
      ///{
      ///    public static List<計算資產編號Result> Call計算資產編號(this SqlConnection conn, string 品項類別)
      ///    {
      ///         var args = new {
      ///             品項類別
      ///         };   
      /// 
      ///        var dataList = conn.Query<計算資產編號Result>("計算資產編號", args,
      ///            commandType: System.Data.CommandType.StoredProcedure).AsList();
      ///        return dataList;
      ///    }
      ///}

      //※ 無參數狀況下會重複，故不產出。
      if (!f_NonParam)
      {
        pocoCode.AppendLine();

        //# 有參數無結果
        if (f_NonResult)
          pocoCode.Append($"public static int Call{proc.SPECIFIC_NAME}(this SqlConnection conn");
        //# 有參數有結果
        else
          pocoCode.Append($"public static List<{proc.SPECIFIC_NAME}Result> Call{proc.SPECIFIC_NAME}(this SqlConnection conn");

        // 加入參數至函式
        proc.ParamList.ForEach(arg =>
        {
          string dataType = arg.IS_TABLE_TYPE == "YES"
            ? $"List<{arg.DATA_TYPE}>"
            : DBHelper.MapNetDataType(arg.DATA_TYPE);

          string nullable = DetermineNullable("YES", dataType, false); // (dataType != "string") ? "?" : "";
          pocoCode.Append($", {dataType}{nullable} {arg.PARAMETER_NAME.Substring(1)}");
        });

        // 加入交易
        pocoCode.AppendLine(", SqlTransaction? txn = null)");
        pocoCode.AppendLine("{"); // begin of: call procedure 

        pocoCode.AppendLine($"{_indent}var args = new {proc.SPECIFIC_NAME}Args {{");
        proc.ParamList.ForEach(arg =>
          pocoCode.AppendLine($"{_indent}{_indent}{arg.PARAMETER_NAME.Substring(1)} = {arg.PARAMETER_NAME.Substring(1)},"));
        pocoCode.AppendLine($"{_indent}}};");
        pocoCode.AppendLine();

        pocoCode.AppendLine($"{_indent}var result = conn.Call{proc.SPECIFIC_NAME}(args, txn); ");
        pocoCode.AppendLine($"{_indent}return result;");
        //------
        pocoCode.AppendLine("}"); // end of: call procedure 
      }

      //---------------------
      pocoCode.AppendLine("}"); // end of: Procedure Instance 
      pocoCode.AppendLine("}"); // end of: Namespace
      pocoCode.AppendLine();

      //## 一個 Procedure 一個檔案
      File.WriteAllText(Path.Combine(outDir.FullName, $"{proc.SPECIFIC_NAME}.cs"), pocoCode.ToString(), encoding: Encoding.UTF8);
      Console.WriteLine(pocoCode.ToString());
    });
  }

  /// <summary>
  /// 產生 TVP 類別。
  /// </summary>
  void GenerateTableTypePocoCode(SqlConnection conn, DirectoryInfo outDir)
  {
    Console.WriteLine("================================================================================");
    Console.WriteLine($"#BEGIN {nameof(GenerateTableTypePocoCode)}");
    var tableList = DBHelper.LoadTableType(conn);
    tableList.ForEach(table =>
    {
      Console.WriteLine("--------------------------------------------------------------------------------");
      Console.WriteLine($"#TABLE TYPE: {table.TABLE_TYPE_SCHEMA}.{table.TABLE_TYPE_NAME}");
      StringBuilder pocoCode = new StringBuilder();

      pocoCode.AppendLine($"namespace {_nameSpace}");
      pocoCode.AppendLine("{");
      pocoCode.AppendLine("using System;");
      pocoCode.AppendLine("using System.ComponentModel.DataAnnotations;");
      pocoCode.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
      pocoCode.AppendLine();

      pocoCode.AppendLine($"public class {table.TABLE_TYPE_NAME} ");
      pocoCode.AppendLine("{"); // begin of: Class

      List<TableTypeColumnInfo> columnList = DBHelper.LoadTableTypeColumn(conn, table.TABLE_TYPE_NAME, table.TABLE_TYPE_SCHEMA);
      columnList.ForEach(col =>
      {
        string dataType = DBHelper.MapNetDataType(col.DATA_TYPE);
        bool isPrimaryKey = false;
        bool isIdentity = col.IS_IDENTITY == "YES";
        bool isComputed = false;
        string nullable = DetermineNullable("YES", dataType, isPrimaryKey); // (dataType != "string" && !isPrimaryKey) ? "?" : ""; // ORM 欄位原則上都是 nullable 不然在 input binding 會很難實作。

        //# Key & Computed attribute
        if (isPrimaryKey)
          pocoCode.AppendLine($"{_indent}[Key]");
        if (isIdentity)
          pocoCode.AppendLine($"{_indent}[DatabaseGenerated(DatabaseGeneratedOption.Identity)]");
        if (isComputed)
          pocoCode.AppendLine($"{_indent}[DatabaseGenerated(DatabaseGeneratedOption.Computed)]");

        //# Required attribute
        if (col.IS_NULLABLE == "NO")
          pocoCode.AppendLine($"{_indent}[Required]");

        //# filed
        string? defaultString = (dataType == "string" && _fieldWithDefault) ? " = default!;" : null;
        pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {col.COLUMN_NAME} {{ get; set; }}{defaultString}");
      });

      //## 產生Copy函式
      ///public void Copy(職員基本表 src)
      ///{
      ///    this.職員代碼 = src.職員代碼;
      ///    this.職員姓名 = src.職員姓名;
      ///    this.狀態 = src.狀態;
      ///}

      pocoCode.AppendLine();
      pocoCode.AppendLine($"{_indent}public void Copy({table.TABLE_TYPE_NAME} src)");
      pocoCode.AppendLine($"{_indent}{{");
      columnList.ForEach(col =>
        pocoCode.AppendLine($"{_indent}{_indent}this.{col.COLUMN_NAME} = src.{col.COLUMN_NAME};"));
      pocoCode.AppendLine($"{_indent}}}"); // end of: Copy

      //## 產生Clone函式
      ///public 職員基本表 Clone()
      ///{
      ///    return new 職員基本表
      ///    {
      ///        職員代碼 = this.職員代碼,
      ///        職員姓名 = this.職員姓名,
      ///        狀態 = this.狀態
      ///    };
      ///}

      pocoCode.AppendLine();
      pocoCode.AppendLine($"{_indent}public {table.TABLE_TYPE_NAME} Clone()");
      pocoCode.AppendLine($"{_indent}{{");
      pocoCode.AppendLine($"{_indent}{_indent}return new {table.TABLE_TYPE_NAME} {{");
      columnList.ForEach(col =>
        pocoCode.AppendLine($"{_indent}{_indent}{_indent}{col.COLUMN_NAME} = this.{col.COLUMN_NAME},"));
      pocoCode.AppendLine($"{_indent}{_indent}}};");
      pocoCode.AppendLine($"{_indent}}}"); // end of: Clone

      //---------------------
      pocoCode.AppendLine("}"); // end of: Class
      pocoCode.AppendLine("}"); // end of: Namespace
      pocoCode.AppendLine();

      //## 一個 TableType 一個檔案
      File.WriteAllText(Path.Combine(outDir.FullName, $"{table.TABLE_TYPE_NAME}.cs"), pocoCode.ToString(), encoding: Encoding.UTF8);
      Console.WriteLine(pocoCode.ToString());
    });
  }

  /// <summary>
  /// 先只考慮有參數且有結果欄位的狀況。
  /// </summary>
  void GenerateTableValuedFunctionPocoCode(SqlConnection conn, DirectoryInfo outDir)
  {
    Console.WriteLine("================================================================================");
    Console.WriteLine($"#BEGIN {nameof(GenerateTableValuedFunctionPocoCode)}");
    var procList = DBHelper.LoadTableValuedFunction(conn);
    procList.ForEach(proc =>
    {
      Console.WriteLine("--------------------------------------------------------------------------------");
      Console.WriteLine($"#{proc.ROUTINE_TYPE}: {proc.SPECIFIC_CATALOG}.{proc.SPECIFIC_SCHEMA}.{proc.SPECIFIC_NAME}");
      StringBuilder pocoCode = new StringBuilder();

      pocoCode.AppendLine($"namespace {_nameSpace}");
      pocoCode.AppendLine("{");
      pocoCode.AppendLine("using System;");
      pocoCode.AppendLine("using System.Collections.Generic;");
      pocoCode.AppendLine("using Dapper;");
      pocoCode.AppendLine($"using {_sqlClientLibrary};");
      pocoCode.AppendLine();

      //## Procedure Result Class ------------
      ///public class 計算資產編號Result
      ///{
      ///    public string 資產編號 { get; set; }
      ///}

      pocoCode.AppendLine($"public class {proc.SPECIFIC_NAME}Result ");
      pocoCode.AppendLine("{");
      proc.ColumnList.ForEach(col =>
      {
        string dataType = DBHelper.MapNetDataType(col.DATA_TYPE);
        string nullable = DetermineNullable(col.IS_NULLABLE, dataType, false); // (dataType != "string" && col.IS_NULLABLE == "YES") ? "?" : "";
        string? defaultString = (dataType == "string" && _fieldWithDefault) ? " = default!;" : null;

        pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {col.COLUMN_NAME} {{ get; set; }}{defaultString}");
      });
      pocoCode.AppendLine("}"); // end of: Reslt Column 

      //## Procedure Parameter Class ------------
      ///public class 計算資產編號Args
      ///{
      ///    public string 品項類別 { get; set; }
      ///}
      ///
      pocoCode.AppendLine();
      pocoCode.AppendLine($"public class {proc.SPECIFIC_NAME}Args ");
      pocoCode.AppendLine("{");
      proc.ParamList.ForEach(arg =>
      {
        string dataType = DBHelper.MapNetDataType(arg.DATA_TYPE);
        string nullable = DetermineNullable("YES", dataType, false); // (dataType != "string") ? "?" : "";
        string? defaultString = (dataType == "string" && _fieldWithDefault) ? " = default!;" : null;

        pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {arg.PARAMETER_NAME.Substring(1)} {{ get; set; }}{defaultString}");
      });
      pocoCode.AppendLine("}"); // end of: Reslt Column 

      pocoCode.AppendLine();
      pocoCode.AppendLine("static partial class DBHelperClassExtensions");
      pocoCode.AppendLine("{");
      //---------------------
      //※ 先只考慮有參數且有結果欄位的狀況。

      //## Procedure Instance ------------
      ///public static List<查詢電腦設備領用裝況Result> Call查詢電腦設備領用裝況(this SqlConnection conn, 查詢電腦設備領用裝況Args args, SqlTransaction txn = null)
      ///{
      ///    string sql = @"SELECT * FROM [dbo].[查詢電腦設備領用裝況](@帳卡編號,@使用者代碼,@領用起日,@領用訖日,@領用狀況); ";
      ///    var dataList = conn.Query<查詢電腦設備領用裝況Result>(sql, args, txn).AsList();
      ///    return dataList;
      ///}

      pocoCode.AppendLine($"public static List<{proc.SPECIFIC_NAME}Result> Call{proc.SPECIFIC_NAME}(this SqlConnection conn, {proc.SPECIFIC_NAME}Args args, SqlTransaction? txn = null)");
      pocoCode.AppendLine("{");

      pocoCode.Append($"{_indent}var sql = @\"SELECT * FROM [{proc.SPECIFIC_SCHEMA}].[{proc.SPECIFIC_NAME}](");
      pocoCode.Append(String.Join(",", proc.ParamList.Select(c => c.PARAMETER_NAME)));
      pocoCode.AppendLine($"); \"; ");

      pocoCode.AppendLine($"{_indent}var dataList = conn.Query<{proc.SPECIFIC_NAME}Result>(sql, args, txn).AsList();");
      pocoCode.AppendLine($"{_indent}return dataList;");
      pocoCode.AppendLine("}");

      //## Procedure Instance : method 2 直接帶入參數 ------------
      ///public static List<查詢電腦設備領用裝況Result> Call查詢電腦設備領用裝況(this SqlConnection conn, string 帳卡編號, string 使用者代碼, DateTime? 領用起日, DateTime? 領用訖日, string 領用狀況)
      ///{
      ///    var args = {
      ///        帳卡編號,使用者代碼,領用起日,領用訖日,領用狀況
      ///    };
      ///    string sql = @"SELECT * FROM [dbo].[查詢電腦設備領用裝況](@帳卡編號,@使用者代碼,@領用起日,@領用訖日,@領用狀況); ";
      ///    var dataList = conn.Query<查詢電腦設備領用裝況Result>(sql, args).AsList();
      ///    return dataList;
      ///}

      pocoCode.AppendLine();
      pocoCode.Append($"public static List<{proc.SPECIFIC_NAME}Result> Call{proc.SPECIFIC_NAME}(this SqlConnection conn");
      proc.ParamList.ForEach(arg =>
      {
        string dataType = DBHelper.MapNetDataType(arg.DATA_TYPE);
        string nullable = DetermineNullable("YES", dataType, false); // (dataType != "string") ? "?" : "";

        pocoCode.Append($", {dataType}{nullable} {arg.PARAMETER_NAME.Substring(1)}");
      });
      pocoCode.AppendLine(", SqlTransaction? txn = null)");
      pocoCode.AppendLine("{");
      pocoCode.AppendLine($"{_indent}var args = new {{");
      proc.ParamList.ForEach(arg =>
        pocoCode.AppendLine($"{_indent}{_indent}{arg.PARAMETER_NAME.Substring(1)},"));
      pocoCode.AppendLine($"{_indent}}};");

      pocoCode.AppendLine();
      pocoCode.Append($"{_indent}var sql = @\"SELECT * FROM [{proc.SPECIFIC_SCHEMA}].[{proc.SPECIFIC_NAME}](");
      pocoCode.Append(String.Join(",", proc.ParamList.Select(c => c.PARAMETER_NAME)));
      pocoCode.AppendLine($"); \"; ");

      pocoCode.AppendLine($"{_indent}var dataList = conn.Query<{proc.SPECIFIC_NAME}Result>(sql, args, txn).AsList();");
      pocoCode.AppendLine($"{_indent}return dataList;");
      pocoCode.AppendLine("}");

      //---------------------
      pocoCode.AppendLine("}"); // end of: DBHelperClassExtensions 
      pocoCode.AppendLine("}"); // end of: Namespace
      pocoCode.AppendLine();

      //## 一個 Procedure 一個檔案
      File.WriteAllText(Path.Combine(outDir.FullName, $"{proc.SPECIFIC_NAME}.cs"), pocoCode.ToString(), encoding: Encoding.UTF8);
      Console.WriteLine(pocoCode.ToString());
    });
  }

  void GenerateTableToExcel(SqlConnection conn, DirectoryInfo outDir)
  {
    Console.WriteLine("================================================================================");
    Console.WriteLine($"#BEGIN {nameof(GenerateTableToExcel)}");

    // 以 Excel 範本為基礎
    using var workbook = new XLWorkbook(@"Template/Template_Overview.xlsx");

    var tableList = DBHelper.LoadTable(conn);

    #region 資料庫檔案(物件)總覽

    OverviewInfo overview = new()
    {
      DbName = conn.Database,
      PrintDate = $"{DateTime.Now:yyyy-MM-dd}",
      ItemList = tableList.Select((c, idx) => new OverviewItem
      {
        Sn = $"{idx}",
        Name = c.TABLE_NAME,
        Desc = c.MS_Description,
        Type = c.TABLE_TYPE
      }).ToList()
    };

    using var overviewTpl = new XLTemplate(@"Template/Template_Overview.xlsx");
    overviewTpl.AddVariable(overview);
    overviewTpl.Generate();

    //overviewTpl.SaveAs(fi.FullName);
    workbook.Worksheet("Schema Overview").Delete();
    overviewTpl.Workbook.Worksheet(1).CopyTo(workbook, "Schema Overview");

    #endregion 資料庫檔案(物件)總覽

    #region 一一加入資料庫檔案(物件)明細
    tableList.ForEach(table =>
    {
      //## 一個 Table 一個 sheet
      List<ColumnInfo> columnList = DBHelper.LoadTableColumn(conn, table.TABLE_NAME);

      RptTableInfo tableInfo = new RptTableInfo
      {
        Name = table.TABLE_NAME,
        Type = table.TABLE_TYPE,
        Desc = table.MS_Description,
        PrintDate = $"{DateTime.Now:yyyy-MM-dd}",
        FieldList = columnList.Select((c, idx) => new RptTableField
        {
          Sn = c.ORDINAL_POSITION,
          Name = c.COLUMN_NAME,
          Cname = c.MS_Description?.Split(':', '：', '\r', '\n')[0].Trim(),
          Type = c.DATA_TYPE,
          Len = "-1".Equals(c.CHARACTER_MAXIMUM_LENGTH) ? "MAX" : c.CHARACTER_MAXIMUM_LENGTH,
          Pk = "YES".Equals(c.IS_PK) ? "pk" : "",
          Default = c.COLUMN_DEFAULT,
          Nullable = c.IS_NULLABLE,
          Desc = c.MS_Description,
        }).ToList()
      };

      using var tableTpl = new XLTemplate(@"Template/Template_Table.xlsx");
      tableTpl.AddVariable(tableInfo);
      tableTpl.Generate();
      tableTpl.Workbook.Worksheet(1).CopyTo(workbook, table.TABLE_NAME);
    });
    #endregion

    //# 為 Overview Sheet 的項目加入 hyper-link 連結到明細
    IXLWorksheet overSheet = workbook.Worksheet("Schema Overview");
    var lastRowNum = overSheet.LastRowUsed().RowNumber();

    var activeRow = overSheet.Row(6);
    while(lastRowNum >= activeRow.RowNumber())
    {
      var link = activeRow.Cell(2).CreateHyperlink();
      link.InternalAddress = $"{activeRow.Cell(2).Value}!A1"; // 連結到自己的明細
      // next
      activeRow = activeRow.RowBelow();
    }

    //# 成功存檔
    var fi = new FileInfo(Path.Combine(outDir.FullName, $"{conn.Database}_Schema.xlsx"));
    if (fi.Exists) fi.Delete();
    workbook.SaveAs(fi.FullName);

    Console.WriteLine("已匯出 Excel。");
  }

  /// <summary>
  /// ※ORM 欄位原則上都是 nullable 不然在 input binding 會很難實作。
  /// </summary>
  /// <returns></returns>
  string DetermineNullable(string IS_NULLABLE, string dataType, bool isPrimaryKey)
  {
    // ORM 欄位原則上都是 nullable 不然在 input binding 會很難實作。
    string nullable = IS_NULLABLE == "YES" ? "?" : string.Empty;

    // `string` 本身就俱 null 特性。加不加`?`符號都一樣。
    if (dataType == "string") return string.Empty;

    // P-Key 一定有值。
    if (isPrimaryKey) return string.Empty;

    // 基於 C# 7.3 語言限制
    if(_cs73Limit)
    {
      if (dataType == "Byte[]") return string.Empty; // 不支援`Byte[]?`語法。
    }

    return nullable;
  }
}
