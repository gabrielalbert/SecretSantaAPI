namespace TaskGame.API.Models;

public enum InvitationStatus
{
    Pending = 1,
    Accepted = 2,
    Declined = 3
}

public class EventInvitation
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public Guid ChrisMaUserId { get; set; }
    public Guid ChrisChildUserId { get; set; }
    public DateTime InvitedAt { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTime? ResponseAt { get; set; }

    // Navigation properties
    public Event? Event { get; set; }
    public User? User { get; set; }
    public User? ChrisMaUser { get; set; }
    public User? ChrisChildUser { get; set; }
}
