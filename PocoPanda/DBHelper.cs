using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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