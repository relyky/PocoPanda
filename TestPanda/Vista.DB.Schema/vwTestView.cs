namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("vwTestView")]
public class vwTestView 
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Required]
  public Int64? SN { get; set; }
  [Required]
  public string IDN { get; set; } = default!;
  [Required]
  public string Title { get; set; } = default!;
  [Required]
  public Decimal? Amount { get; set; }

  public void Copy(vwTestView src)
  {
    this.SN = src.SN;
    this.IDN = src.IDN;
    this.Title = src.Title;
    this.Amount = src.Amount;
  }

  public vwTestView Clone()
  {
    return new vwTestView {
      SN = this.SN,
      IDN = this.IDN,
      Title = this.Title,
      Amount = this.Amount,
    };
  }
}
}

