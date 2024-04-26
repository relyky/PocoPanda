namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("MyJson")]
public class MyJson 
{
  public string FormContent { get; set; } = default!;
  /// <summary>
  /// 表單編號
  /// Computed Definition: (json_value([FormContent],'$.FormNo'))
  /// </summary>
  [Display(Name = "表單編號")]
  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public string vFormNo { get; set; } = default!;
  /// <summary>
  /// 表單內容簡述
  /// Computed Definition: (json_value([FormContent],'$.Description'))
  /// </summary>
  [Display(Name = "表單內容簡述")]
  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public string vDescription { get; set; } = default!;
  /// <summary>
  /// 申請單狀態
  /// Computed Definition: (json_value([FormContent],'$.Status'))
  /// </summary>
  [Display(Name = "申請單狀態")]
  [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
  public string vStatus { get; set; } = default!;

  public void Copy(MyJson src)
  {
    this.FormContent = src.FormContent;
    this.vFormNo = src.vFormNo;
    this.vDescription = src.vDescription;
    this.vStatus = src.vStatus;
  }

  public MyJson Clone()
  {
    return new MyJson {
      FormContent = this.FormContent,
      vFormNo = this.vFormNo,
      vDescription = this.vDescription,
      vStatus = this.vStatus,
    };
  }
}
}

