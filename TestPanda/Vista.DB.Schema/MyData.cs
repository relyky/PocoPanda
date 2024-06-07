namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// 測試資料表: 專門設計測試 PocoPando。
/// </summary>
[Table("MyData")]
public class MyData 
{
  /// <summary>
  /// 系統序號
  /// </summary>
  [Display(Name = "系統序號")]
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Required]
  public Int64 SN { get; set; }
  /// <summary>
  /// 身份證號
  /// </summary>
  [Display(Name = "身份證號")]
  [Required]
  public string IDN { get; set; } = default!;
  /// <summary>
  /// 抬頭:這是抬頭
  /// </summary>
  [Display(Name = "抬頭")]
  [Required]
  public string Title { get; set; } = default!;
  /// <summary>
  /// 金額：模擬數值
  /// </summary>
  [Display(Name = "金額")]
  [Required]
  public Decimal? Amount { get; set; }
  /// <summary>
  /// 生日：測試只填日期
  /// </summary>
  [Display(Name = "生日")]
  public DateTime? Birthday { get; set; }
  /// <summary>
  /// 起床時間: 測試只填時間
  /// </summary>
  [Display(Name = "起床時間")]
  public TimeSpan? WakeTime { get; set; }
  /// <summary>
  /// 備註  ：  寫些雜七雜八的
  /// </summary>
  [Display(Name = "備註")]
  public string Remark { get; set; } = default!;
  /// <summary>
  /// 系統紀錄時間
  /// </summary>
  [Display(Name = "系統紀錄時間")]
  public DateTime? LogDtm { get; set; }

  public void Copy(MyData src)
  {
    this.SN = src.SN;
    this.IDN = src.IDN;
    this.Title = src.Title;
    this.Amount = src.Amount;
    this.Birthday = src.Birthday;
    this.WakeTime = src.WakeTime;
    this.Remark = src.Remark;
    this.LogDtm = src.LogDtm;
  }

  public MyData Clone()
  {
    return new MyData {
      SN = this.SN,
      IDN = this.IDN,
      Title = this.Title,
      Amount = this.Amount,
      Birthday = this.Birthday,
      WakeTime = this.WakeTime,
      Remark = this.Remark,
      LogDtm = this.LogDtm,
    };
  }
}
}

