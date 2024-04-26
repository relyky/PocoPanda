namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("MyProduct")]
public class MyProduct 
{
  /// <summary>
  /// 序號
  /// </summary>
  [Display(Name = "序號")]
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Required]
  public int Sn { get; set; }
  /// <summary>
  /// 抬頭
  /// </summary>
  [Display(Name = "抬頭")]
  [Required]
  public string Title { get; set; } = default!;
  /// <summary>
  /// 狀態: Enable | Disable
  /// </summary>
  [Display(Name = "狀態")]
  [Required]
  public string Status { get; set; } = default!;

  public void Copy(MyProduct src)
  {
    this.Sn = src.Sn;
    this.Title = src.Title;
    this.Status = src.Status;
  }

  public MyProduct Clone()
  {
    return new MyProduct {
      Sn = this.Sn,
      Title = this.Title,
      Status = this.Status,
    };
  }
}
}

