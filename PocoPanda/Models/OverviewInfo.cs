namespace PocoPanda.Models;

internal class OverviewInfo
{
  public string DbName { get; set; } = default!;
  public string PrintDate { get; set; } = default!;
  public List<OverviewItem> ItemList { get; set; } = default!;
}

internal class OverviewItem
{
  public string Sn { get; set; } = default!;
  public string Name { get; set; } = default!;
  public string Desc { get; set; } = default!;
  public string Type { get; set; } = default!;
}

//-------------------------------------
internal class RptTableInfo
{
  public string Name { get; set; } = default!;
  public string Type { get; set; } = default!;
  public string Desc { get; set; } = default!;
  public string PrintDate { get; set; } = default!;
  public List<RptTableField> FieldList { get; set; } = default!;
}

internal class RptTableField
{
  public string Sn { get; set; } = default!;
  public string Name { get; set; } = default!;
  public string? Cname { get; set; }
  public string Type { get; set; } = default!;
  public string? Len { get; set; }
  public string Pk { get; set; } = default!;
  public string? Default { get; set; }
  public string Nullable { get; set; } = default!;
  public string? Desc { get; set; }
}