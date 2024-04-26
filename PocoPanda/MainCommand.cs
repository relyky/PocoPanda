using Cocona;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
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
    Console.WriteLine($"#BEGIN {nameof(GenerateTablePocoCode)}");


  }
}
