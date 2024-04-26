namespace Vista.DB.Schema
{
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("AttachFile")]
public class AttachFile 
{
  [Key]
  [Required]
  public Guid FileId { get; set; }
  [Required]
  public string FileName { get; set; } = default!;
  [Required]
  public string FileType { get; set; } = default!;
  [Required]
  public string Category { get; set; } = default!;
  [Required]
  public Byte[]? Blob { get; set; }

  public void Copy(AttachFile src)
  {
    this.FileId = src.FileId;
    this.FileName = src.FileName;
    this.FileType = src.FileType;
    this.Category = src.Category;
    this.Blob = src.Blob;
  }

  public AttachFile Clone()
  {
    return new AttachFile {
      FileId = this.FileId,
      FileName = this.FileName,
      FileType = this.FileType,
      Category = this.Category,
      Blob = this.Blob,
    };
  }
}
}

