namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("EXAM_APP_DTL")]
public class EXAM_APP_DTL 
{
  [Key]
  [Required]
  public string APP_NO { get; set; } = default!;
  [Key]
  [Required]
  public int ITEM_NO { get; set; }
  [Required]
  public Decimal? UNIT_PRICE { get; set; }
  public string FEE_CODE { get; set; } = default!;
  [Required]
  public int? UNIT_SALE { get; set; }
  [Required]
  public string SELF_PAY_FLAG { get; set; } = default!;

  public void Copy(EXAM_APP_DTL src)
  {
    this.APP_NO = src.APP_NO;
    this.ITEM_NO = src.ITEM_NO;
    this.UNIT_PRICE = src.UNIT_PRICE;
    this.FEE_CODE = src.FEE_CODE;
    this.UNIT_SALE = src.UNIT_SALE;
    this.SELF_PAY_FLAG = src.SELF_PAY_FLAG;
  }

  public EXAM_APP_DTL Clone()
  {
    return new EXAM_APP_DTL {
      APP_NO = this.APP_NO,
      ITEM_NO = this.ITEM_NO,
      UNIT_PRICE = this.UNIT_PRICE,
      FEE_CODE = this.FEE_CODE,
      UNIT_SALE = this.UNIT_SALE,
      SELF_PAY_FLAG = this.SELF_PAY_FLAG,
    };
  }
}
}

