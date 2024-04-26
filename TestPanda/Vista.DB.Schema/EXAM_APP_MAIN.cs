namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("EXAM_APP_MAIN")]
public class EXAM_APP_MAIN 
{
  [Key]
  [Required]
  public string APP_NO { get; set; } = default!;
  [Required]
  public string APP_NAME { get; set; } = default!;
  [Required]
  public DateTime? EFF_DATE { get; set; }
  public string OFF_DATE { get; set; } = default!;
  [Required]
  public string OFF_FLAG { get; set; } = default!;
  public string SALE_NO { get; set; } = default!;
  public string COMPANY { get; set; } = default!;
  [Required]
  public Decimal? UNIT_PRICE { get; set; }
  [Required]
  public string PAY_TYPE { get; set; } = default!;

  public void Copy(EXAM_APP_MAIN src)
  {
    this.APP_NO = src.APP_NO;
    this.APP_NAME = src.APP_NAME;
    this.EFF_DATE = src.EFF_DATE;
    this.OFF_DATE = src.OFF_DATE;
    this.OFF_FLAG = src.OFF_FLAG;
    this.SALE_NO = src.SALE_NO;
    this.COMPANY = src.COMPANY;
    this.UNIT_PRICE = src.UNIT_PRICE;
    this.PAY_TYPE = src.PAY_TYPE;
  }

  public EXAM_APP_MAIN Clone()
  {
    return new EXAM_APP_MAIN {
      APP_NO = this.APP_NO,
      APP_NAME = this.APP_NAME,
      EFF_DATE = this.EFF_DATE,
      OFF_DATE = this.OFF_DATE,
      OFF_FLAG = this.OFF_FLAG,
      SALE_NO = this.SALE_NO,
      COMPANY = this.COMPANY,
      UNIT_PRICE = this.UNIT_PRICE,
      PAY_TYPE = this.PAY_TYPE,
    };
  }
}
}

