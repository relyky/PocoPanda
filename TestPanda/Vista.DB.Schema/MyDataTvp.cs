namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class MyDataTvp 
{
  [Required]
  public string IDN { get; set; } = default!;
  [Required]
  public string Title { get; set; } = default!;
  [Required]
  public Decimal? Amount { get; set; }
  public DateTime? Birthday { get; set; }

  public void Copy(MyDataTvp src)
  {
    this.IDN = src.IDN;
    this.Title = src.Title;
    this.Amount = src.Amount;
    this.Birthday = src.Birthday;
  }

  public MyDataTvp Clone()
  {
    return new MyDataTvp {
      IDN = this.IDN,
      Title = this.Title,
      Amount = this.Amount,
      Birthday = this.Birthday,
    };
  }
}
}

