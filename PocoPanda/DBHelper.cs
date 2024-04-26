﻿using Dapper;
using Microsoft.Data.SqlClient;

namespace PocoPanda;

class DBHelper
{
  /// <summary>
  /// SQL data type → NET data type
  /// </summary>
  public static string MapNetDataType(string sqlDataTypeName)
  {
    switch (sqlDataTypeName.ToLower())
    {
      case "bigint":
        return "Int64";
      case "binary":
        return "Byte[]";
      case "bit":
        return "bool";
      //case "char":
      case string t when t.StartsWith("char"):
        return "string";
      case "cursor":
        return string.Empty;
      case "datetime":
        return "DateTime";
      case "datetime2":
        return "DateTime";
      case "decimal":
        return "Decimal";
      case "float":
        return "Double";
      case "int":
        return "int";
      case "money":
        return "Decimal";
      //case "nchar":
      case string t when t.StartsWith("nchar"):
        return "string";
      case "numeric":
        return "Decimal";
      //case "nvarchar":
      case string t when t.StartsWith("nvarchar"):
        return "string";
      case "real":
        return "single";
      case "smallint":
        return "Int16";
      case "text":
        return "string";
      case "tinyint":
        return "Byte";
      case "varbinary":
        return "Byte[]";
      case "xml":
        return "string";
      //case "varchar":
      case string t when t.StartsWith("varchar"):
        return "string";
      case "smalldatetime":
        return "DateTime";
      case "image":
        return "byte[]";
      case "uniqueidentifier":
        return "Guid";
      case "datetimeoffset":
        return "DateTimeOffset";
      default:
        return $"{sqlDataTypeName}:not_support"; // not support
    }
  }

  public static List<TableInfo> LoadTable(SqlConnection conn)
  {
    string sql = @"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME != 'sysdiagrams' ";
    var tableList = conn.Query<TableInfo>(sql).ToList();
    return tableList;
  }

  public static List<ColumnInfo> LoadTableColumn(SqlConnection conn, string tableName, string tableSchema = "dbo")
  {
    string sql = $@"WITH PK AS (
SELECT TC.CONSTRAINT_CATALOG,TC.CONSTRAINT_SCHEMA,TC.CONSTRAINT_NAME,TC.TABLE_NAME,TC.CONSTRAINT_TYPE, KC.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KC ON 
	TC.CONSTRAINT_CATALOG = KC.CONSTRAINT_CATALOG AND 
	TC.CONSTRAINT_SCHEMA = KC.CONSTRAINT_SCHEMA AND
	TC.CONSTRAINT_NAME = KC.CONSTRAINT_NAME
WHERE TC.CONSTRAINT_TYPE = 'PRIMARY KEY'
)
, MSDESC AS (
select ss.name [TABLE_SCHEMA], st.name [TABLE_NAME], sc.name [COLUMN_NAME], sep.value [MS_Description]
from sys.tables st
inner join sys.columns sc on st.object_id = sc.object_id
inner join sys.schemas ss on st.schema_id = ss.schema_id
inner join sys.extended_properties sep on 
  st.object_id = sep.major_id and 
  sc.column_id = sep.minor_id and 
  sep.name = 'MS_Description' and 
  sep.value is not null
)
, COMPUTED AS (
select ss.name [TABLE_SCHEMA], st.name [TABLE_NAME], sc.name [COLUMN_NAME]
, sc.is_computed [IS_COMPUTED]
, sc.definition [COMPUTED_DEFINITION]
from sys.tables st
inner join sys.computed_columns sc on st.object_id = sc.object_id
inner join sys.schemas ss on st.schema_id = ss.schema_id
where sc.is_computed = 1
)
SELECT C.COLUMN_NAME, C.ORDINAL_POSITION, C.TABLE_CATALOG, C.TABLE_SCHEMA, C.TABLE_NAME, C.IS_NULLABLE, C.DATA_TYPE, C.CHARACTER_MAXIMUM_LENGTH, C.COLUMN_DEFAULT
, IS_IDENTITY = CASE WHEN COLUMNPROPERTY(object_id(C.TABLE_SCHEMA+'.'+C.TABLE_NAME), C.COLUMN_NAME, 'IsIdentity') = 1 THEN 'YES' ELSE 'NO' END
, IS_PK = CASE WHEN PK.CONSTRAINT_TYPE = 'PRIMARY KEY' THEN 'YES' ELSE 'NO' END
, MS_Description = MSDESC.MS_Description
, IS_COMPUTED = CASE WHEN COMPUTED.IS_COMPUTED = 1 THEN 'YES' ELSE 'NO' END
, COMPUTED.COMPUTED_DEFINITION
FROM INFORMATION_SCHEMA.COLUMNS C
LEFT JOIN PK ON C.COLUMN_NAME = PK.COLUMN_NAME AND
  C.TABLE_NAME = PK.TABLE_NAME AND
  C.TABLE_SCHEMA = PK.CONSTRAINT_SCHEMA AND
  C.TABLE_CATALOG = PK.CONSTRAINT_CATALOG
LEFT JOIN MSDESC ON C.COLUMN_NAME = MSDESC.COLUMN_NAME AND
  C.TABLE_NAME = MSDESC.TABLE_NAME AND
  C.TABLE_SCHEMA = MSDESC.TABLE_SCHEMA
LEFT JOIN COMPUTED ON C.COLUMN_NAME = COMPUTED.COLUMN_NAME AND
  C.TABLE_NAME = COMPUTED.TABLE_NAME AND
  C.TABLE_SCHEMA = COMPUTED.TABLE_SCHEMA
WHERE C.TABLE_NAME = @tableName
 AND C.TABLE_SCHEMA = @tableSchema
ORDER BY TABLE_NAME, ORDINAL_POSITION ASC ";

    var columnList = conn.Query<ColumnInfo>(sql, new { tableSchema, tableName }).ToList();
    return columnList;
  }

  public static List<RoutineInfo> LoadProcedure(SqlConnection conn)
  {
    string sql1 = @"SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME, ROUTINE_TYPE
  FROM INFORMATION_SCHEMA.ROUTINES
 WHERE ROUTINE_TYPE = 'PROCEDURE'
   AND ROUTINE_NAME NOT IN('sp_upgraddiagrams','sp_helpdiagrams','sp_helpdiagramdefinition','sp_creatediagram','sp_renamediagram','sp_alterdiagram','sp_dropdiagram'); ";

    List<RoutineInfo> procedureList = new List<RoutineInfo>();
    foreach (var info in conn.Query<RoutineInfo>(sql1).ToList())
    {
      // parameter info
      info.ParamList = DoLoadParameterInfo(conn, info.SPECIFIC_NAME, info.SPECIFIC_SCHEMA, info.SPECIFIC_CATALOG);

      // result column info
      info.ColumnList = conn.Query("sp_describe_first_result_set", new { tsql = info.SPECIFIC_NAME },
          commandType: System.Data.CommandType.StoredProcedure).Select(c => new RoutineColumnInfo
          {
            COLUMN_NAME = (string)c.name,
            ORDINAL_POSITION = (int)c.column_ordinal,
            IS_NULLABLE = (bool)c.is_nullable ? "YES" : "NO",
            DATA_TYPE = (string)c.system_type_name
          }).ToList();

      procedureList.Add(info);
    }

    return procedureList;
  }

  static List<ParameterInfo> DoLoadParameterInfo(SqlConnection conn, string SPECIFIC_NAME, string SPECIFIC_SCHEMA, string SPECIFIC_CATALOG)
  {
    string sql2 = @"SELECT SPECIFIC_CATALOG, SPECIFIC_SCHEMA, SPECIFIC_NAME, ORDINAL_POSITION, PARAMETER_NAME
, [DATA_TYPE] = IIF(DATA_TYPE = 'table type', USER_DEFINED_TYPE_NAME, DATA_TYPE)
, IS_TABLE_TYPE = IIF(DATA_TYPE = 'table type', 'YES', 'NO')
 FROM INFORMATION_SCHEMA.PARAMETERS
 WHERE SPECIFIC_NAME = @SPECIFIC_NAME
  AND SPECIFIC_SCHEMA = @SPECIFIC_SCHEMA
  AND SPECIFIC_CATALOG = @SPECIFIC_CATALOG; ";

    var paramList = conn.Query<ParameterInfo>(sql2, new { SPECIFIC_NAME, SPECIFIC_SCHEMA, SPECIFIC_CATALOG }).AsList();
    return paramList;
  }
}

record TableInfo
{
  public string TABLE_CATALOG { get; set; } = default!;
  public string TABLE_SCHEMA { get; set; } = default!;
  public string TABLE_NAME { get; set; } = default!;
  public string TABLE_TYPE { get; set; } = default!;
}

record ColumnInfo
{
  public string COLUMN_NAME { get; set; } = default!;
  public string ORDINAL_POSITION { get; set; } = default!;
  public string TABLE_CATALOG { get; set; } = default!;
  public string TABLE_SCHEMA { get; set; } = default!;
  public string TABLE_NAME { get; set; } = default!;
  public string DATA_TYPE { get; set; } = default!;
  public string? CHARACTER_MAXIMUM_LENGTH { get; set; }
  public string? COLUMN_DEFAULT { get; set; }
  public string IS_NULLABLE { get; set; } = default!;
  public string IS_IDENTITY { get; set; } = default!;
  public string IS_PK { get; set; } = default!;
  public string? MS_Description { get; set; }
  public string IS_COMPUTED { get; set; } = default!;
  public string? COMPUTED_DEFINITION { get; set; }
}

class RoutineInfo
{
  public string SPECIFIC_CATALOG { get; set; } = string.Empty;
  public string SPECIFIC_SCHEMA { get; set; } = string.Empty;
  public string SPECIFIC_NAME { get; set; } = string.Empty;
  public string ROUTINE_TYPE { get; set; } = string.Empty;

  public List<ParameterInfo> ParamList { get; set; }
  public List<RoutineColumnInfo> ColumnList { get; set; }
}

class ParameterInfo
{
  public string PARAMETER_NAME { get; set; }
  public int ORDINAL_POSITION { get; set; }
  public string SPECIFIC_CATALOG { get; set; }
  public string SPECIFIC_SCHEMA { get; set; }
  public string PECIFIC_NAME { get; set; }
  public string DATA_TYPE { get; set; }
  public string IS_TABLE_TYPE { get; set; }
}

class RoutineColumnInfo
{
  public string COLUMN_NAME { get; set; }
  public int ORDINAL_POSITION { get; set; }
  public string IS_NULLABLE { get; set; }
  public string DATA_TYPE { get; set; }
}