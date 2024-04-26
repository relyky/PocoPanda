using Cocona;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
  readonly string _sqlClientLibrary = "Microsoft.Data.SqlClient";

  [RequiresUnreferencedCode("Calls Microsoft.Extensions.Configuration.ConfigurationBinder.GetValue<T>(String, T)")]
  public MainCommand(IConfiguration config)
  {
    //# 取得參數
    _connStr = config.GetConnectionString("DefaultConnection");
    _nameSpace = config["Namespace"];
    _outputFolder = config["OutputFolder"];
    _exportExcel = config.GetValue<bool>("ExportExcel", false);
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
    Console.WriteLine();

    try
    {
      //# 建立輸出目錄
      DirectoryInfo outDir = new DirectoryInfo(_outputFolder);
      if (!outDir.Exists) outDir.Create();

      //# 開始產生 POCO classes 
      using var conn = new SqlConnection(_connStr);
      await conn.OpenAsync();

      //GenerateTablePocoCode(conn, outDir); // in dev 暫成拿掉 
      GenerateProcPocoCode(conn, outDir);
      //GenerateTableValuedFunctionPocoCode
      //GenerateTableTypePocoCode
      //GenerateTableToExcel

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

      pocoCode.AppendLine($"[Table(\"{table.TABLE_NAME}\")]");
      pocoCode.AppendLine($"public class {table.TABLE_NAME} ");
      pocoCode.AppendLine("{");
      //---------------------
      var columnList = DBHelper.LoadTableColumn(conn, table.TABLE_NAME, table.TABLE_SCHEMA);
      columnList.ForEach(col =>
      {
        string dataType = DBHelper.MapNetDataType(col.DATA_TYPE);
        bool isPrimaryKey = col.IS_PK == "YES";
        bool isIdentity = col.IS_IDENTITY == "YES";
        bool isComputed = col.IS_COMPUTED == "YES";
        string nullable = (dataType != "string" && !isPrimaryKey) ? "?" : string.Empty; // ORM 欄位原則上都是 nullable 不然在 input binding 會很難實作。
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
        pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {col.COLUMN_NAME} {{ get; set; }}");
      });

      //## 產生Copy函式
      ///public void Copy(職員基本表 src)
      ///{
      ///    this.職員代碼 = src.職員代碼;
      ///    this.職員姓名 = src.職員姓名;
      ///    this.狀態 = src.狀態;
      ///}

      pocoCode.AppendLine();
      pocoCode.AppendLine($"{_indent}public void Copy({table.TABLE_NAME} src)");
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
      pocoCode.AppendLine($"{_indent}public {table.TABLE_NAME} Clone()");
      pocoCode.AppendLine($"{_indent}{{");
      pocoCode.AppendLine($"{_indent}{_indent}return new {table.TABLE_NAME} {{");
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
      pocoCode.AppendLine("using System.ComponentModel.DataAnnotations;");
      pocoCode.AppendLine("using Dapper;");
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
          string nullable = (dataType != "string" && col.IS_NULLABLE == "YES") ? "?" : "";

          pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {col.COLUMN_NAME} {{ get; set; }}");
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

          string nullable = (dataType != "string") ? "?" : "";
          pocoCode.AppendLine($"{_indent}public {dataType}{nullable} {arg.PARAMETER_NAME.Substring(1)} {{ get; set; }}");
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
          pocoCode.AppendLine($"public static int Call{proc.SPECIFIC_NAME}(this SqlConnection conn, SqlTransaction txn = null)");
        else // 無參數無結果
          pocoCode.AppendLine($"public static List<{proc.SPECIFIC_NAME}Result> Call{proc.SPECIFIC_NAME}(this SqlConnection conn, SqlTransaction txn = null)");
      }
      else
      {
        if (f_NonResult) // 有參數無結果
          pocoCode.AppendLine($"public static int Call{proc.SPECIFIC_NAME}(this SqlConnection conn, {proc.SPECIFIC_NAME}Args args, SqlTransaction txn = null)");
        else // 有參數有結果
          pocoCode.AppendLine($"public static List<{proc.SPECIFIC_NAME}Result> Call{proc.SPECIFIC_NAME}(this SqlConnection conn, {proc.SPECIFIC_NAME}Args args, SqlTransaction txn = null)");
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
        /// param.Add("@dataList", args.dataList.AsDataTable().AsTableValuedParameter(nameof(MyDataTvp));
        /// param.Add("@foo", args.foo);
        pocoCode.AppendLine($"{_indent}var param = new DynamicParameters();");
        proc.ParamList.ForEach(arg =>
        {
          if (arg.IS_TABLE_TYPE == "YES")
            pocoCode.AppendLine($"{_indent}param.Add(\"{arg.PARAMETER_NAME}\", args.{arg.PARAMETER_NAME.Substring(1)}.AsDataTable().AsTableValuedParameter(nameof({arg.DATA_TYPE}))); ");
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

          string nullable = (dataType != "string") ? "?" : "";
          pocoCode.Append($", {dataType}{nullable} {arg.PARAMETER_NAME.Substring(1)}");
        });

        // 加入交易
        pocoCode.AppendLine(", SqlTransaction txn = null)");
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
      pocoCode.AppendLine("} // end of: DBHelperClassExtensions "); // end of: Procedure Instance 
      pocoCode.AppendLine("} // end of: Namespace"); // end of: Namespace
      pocoCode.AppendLine();

      //## 一個 Procedure 一個檔案
      File.WriteAllText(Path.Combine(outDir.FullName, $"{proc.SPECIFIC_NAME}.cs"), pocoCode.ToString(), encoding: Encoding.UTF8);
      Console.WriteLine(pocoCode.ToString());
    });
  }
}

//void GenerateXXXXXX(SqlConnection conn, DirectoryInfo outDir)
//{
//  Console.WriteLine("================================================================================");
//  Console.WriteLine($"#BEGIN {nameof(GenerateXXXXXX)}");

//  Console.WriteLine("--------------------------------------------------------------------------------");
//  Console.WriteLine($"#{table.TABLE_TYPE}: {table.TABLE_CATALOG}.{table.TABLE_SCHEMA}.{table.TABLE_NAME}");
//}