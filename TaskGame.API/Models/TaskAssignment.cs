using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskGame.API.Models;

public class TaskAssignment
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TaskId { get; set; }
    public Guid EventId { get; set; }

    [ForeignKey("TaskId")]
    public virtual TaskItem Task { get; set; } = null!;

    [Required]
    public Guid AssignedToUserId { get; set; }

    [ForeignKey("AssignedToUserId")]
    public virtual User AssignedToUser { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual TaskSubmission? Submission { get; set; }

    public string EventName { get; set; } = string.Empty;
}

public enum TaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Reviewed = 4
}
