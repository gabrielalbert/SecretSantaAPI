using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskGame.API.Models;

public class User
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsAdmin { get; set; } = false;

    // Navigation properties
    public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<TaskAssignment> Assignments { get; set; } = new List<TaskAssignment>();
    public virtual ICollection<TaskSubmission> Submissions { get; set; } = new List<TaskSubmission>();
    public virtual ICollection<Event> CreatedEvents { get; set; } = new List<Event>();
    public virtual ICollection<EventInvitation> Invitations { get; set; } = new List<EventInvitation>();
}
