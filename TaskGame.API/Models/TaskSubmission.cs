using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskGame.API.Models;

public class TaskSubmission
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskAssignmentId { get; set; }

    [ForeignKey("TaskAssignmentId")]
    public virtual TaskAssignment TaskAssignment { get; set; } = null!;

    [Required]
    public Guid SubmittedByUserId { get; set; }

    [ForeignKey("SubmittedByUserId")]
    public virtual User SubmittedBy { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual ICollection<SubmissionFile> Files { get; set; } = new List<SubmissionFile>();
}
