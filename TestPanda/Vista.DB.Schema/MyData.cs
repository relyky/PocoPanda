namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("MyData")]
public class MyData 
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Required]
  public Int64 SN { get; set; }
  [Required]
  public string IDN { get; set; } = default!;
  [Required]
  public string Title { get; set; } = default!;
  [Required]
  public Decimal? Amount { get; set; }
  public DateTime? Birthday { get; set; }
  public string Remark { get; set; } = default!;

  public void Copy(MyData src)
  {
    this.SN = src.SN;
    this.IDN = src.IDN;
    this.Title = src.Title;
    this.Amount = src.Amount;
    this.Birthday = src.Birthday;
    this.Remark = src.Remark;
  }

  public MyData Clone()
  {
    return new MyData {
      SN = this.SN,
      IDN = this.IDN,
      Title = this.Title,
      Amount = this.Amount,
      Birthday = this.Birthday,
      Remark = this.Remark,
    };
  }
}
}

