using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskGame.API.Models;

public class SubmissionFile
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskSubmissionId { get; set; }

    [ForeignKey("TaskSubmissionId")]
    public virtual TaskSubmission TaskSubmission { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(100)]
    public string? ContentType { get; set; }

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
