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
  readonly string _indent = "  ";
  readonly string _sqlClientLibrary = "Microsoft.Data.SqlClient";
  readonly bool _exportExcel;

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

      GenerateTablePocoCode(conn, outDir);
      //GenerateProcPocoCode
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
      //public void Copy(職員基本表 src)
      //{
      //    this.職員代碼 = src.職員代碼;
      //    this.職員姓名 = src.職員姓名;
      //    this.狀態 = src.狀態;
      //}

      pocoCode.AppendLine();
      pocoCode.AppendLine($"{_indent}public void Copy({table.TABLE_NAME} src)");
      pocoCode.AppendLine($"{_indent}{{");
      columnList.ForEach(col =>
                pocoCode.AppendLine($"{_indent}{_indent}this.{col.COLUMN_NAME} = src.{col.COLUMN_NAME};"));
      pocoCode.AppendLine($"{_indent}}}"); // end of: Copy

      //## 產生Clone函式
      //public 職員基本表 Clone()
      //{
      //    return new 職員基本表
      //    {
      //        職員代碼 = this.職員代碼,
      //        職員姓名 = this.職員姓名,
      //        狀態 = this.狀態
      //    };
      //}

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
}
