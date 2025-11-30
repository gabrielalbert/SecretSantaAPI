namespace TaskGame.API.DTOs;

public class CreateTaskDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int Priority { get; set; } = 2;
}

public class TaskDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string? EventName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int Priority { get; set; }
}

public class TaskAssignmentDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid EventId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public int Priority { get; set; }
    public string? EventName { get; set; }
}

public class SubmitTaskDto
{
    public Guid TaskAssignmentId { get; set; }
    public string? Notes { get; set; }
}

public class TaskResultDto
{
    public Guid Id { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public string? Notes { get; set; }
    public string? EventName { get; set; }
    public string SubmittedByUsername { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUsername { get; set; } = string.Empty;
    public DateTime? EventEndDate { get; set; }
    public List<FileDto> Files { get; set; } = new();
}

public class FileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}
