namespace TaskGame.API.DTOs;

public class EventDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int MaxTasksPerUser { get; set; }
    public int InvitedCount { get; set; }
    public int AcceptedCount { get; set; }
}

public class CreateEventDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxTasksPerUser { get; set; } = 5;
    public List<Guid> UserIds { get; set; } = new();
}

public class UpdateEventDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int MaxTasksPerUser { get; set; } = 5;
    public bool IsActive { get; set; }
}

public class EventInvitationDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ChrisMaUsername { get; set; } = string.Empty;
    public string ChrisChildUsername { get; set; } = string.Empty;
    public DateTime InvitedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ResponseAt { get; set; }
}

public class InvitationResponseDto
{
    public bool Accept { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
}

public class UpdateUserDto
{
    public bool? IsActive { get; set; }
    public bool? IsAdmin { get; set; }
}

public class EventDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int MaxTasksPerUser { get; set; }
    public List<EventInvitationDto> Invitations { get; set; } = new();
    public int TaskCount { get; set; }
}
