using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskGame.API.Models;

public class TaskItem
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? DueDate { get; set; }

    [Required]
    public Guid CreatedByUserId { get; set; }

    [ForeignKey("CreatedByUserId")]
    public virtual User CreatedBy { get; set; } = null!;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public int MaxDailyAssignments { get; set; } = 5;

    public Guid? EventId { get; set; }

    [ForeignKey("EventId")]
    public virtual Event? Event { get; set; }

    // Navigation properties
    public virtual ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();
}

public enum TaskPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
