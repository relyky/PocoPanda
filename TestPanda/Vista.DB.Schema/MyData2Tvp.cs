namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class MyData2Tvp 
{
  [Required]
  public string IDN { get; set; } = default!;
  [Required]
  public string Title { get; set; } = default!;
  [Required]
  public string Surname { get; set; } = default!;
  [Required]
  public Decimal? Amount { get; set; }
  public DateTime? Birthday { get; set; }

  public void Copy(MyData2Tvp src)
  {
    this.IDN = src.IDN;
    this.Title = src.Title;
    this.Surname = src.Surname;
    this.Amount = src.Amount;
    this.Birthday = src.Birthday;
  }

  public MyData2Tvp Clone()
  {
    return new MyData2Tvp {
      IDN = this.IDN,
      Title = this.Title,
      Surname = this.Surname,
      Amount = this.Amount,
      Birthday = this.Birthday,
    };
  }
}
}

