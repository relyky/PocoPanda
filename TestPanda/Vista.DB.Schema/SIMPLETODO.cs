namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("SIMPLETODO")]
public class SIMPLETODO 
{
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  [Required]
  public int? SN { get; set; }
  [Required]
  public string TITLE { get; set; } = default!;
  public Decimal? AMOUNT { get; set; }

  public void Copy(SIMPLETODO src)
  {
    this.SN = src.SN;
    this.TITLE = src.TITLE;
    this.AMOUNT = src.AMOUNT;
  }

  public SIMPLETODO Clone()
  {
    return new SIMPLETODO {
      SN = this.SN,
      TITLE = this.TITLE,
      AMOUNT = this.AMOUNT,
    };
  }
}
}

