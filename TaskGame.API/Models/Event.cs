namespace TaskGame.API.Models;

public class Event
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int MaxTasksPerUser { get; set; }

    // Navigation properties
    public User? CreatedBy { get; set; }
    public List<EventInvitation> Invitations { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
}
