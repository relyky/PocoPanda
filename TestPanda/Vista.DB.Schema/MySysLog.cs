namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("MySysLog")]
public class MySysLog 
{
  /// <summary>
  /// 系統序號
  /// </summary>
  [Display(Name = "系統序號")]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Required]
  public Int64? Sn { get; set; }
  /// <summary>
  /// 訊息
  /// </summary>
  [Display(Name = "訊息")]
  [Required]
  public string Message { get; set; } = default!;
  /// <summary>
  /// 系統時間
  /// </summary>
  [Display(Name = "系統時間")]
  [Required]
  public DateTime? SysDtm { get; set; }

  public void Copy(MySysLog src)
  {
    this.Sn = src.Sn;
    this.Message = src.Message;
    this.SysDtm = src.SysDtm;
  }

  public MySysLog Clone()
  {
    return new MySysLog {
      Sn = this.Sn,
      Message = this.Message,
      SysDtm = this.SysDtm,
    };
  }
}
}

