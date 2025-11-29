using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskGame.API.DTOs;
using TaskGame.API.Models;
using TaskGame.API.Repositories;
using TaskGame.API.Services;
using TaskStatus = TaskGame.API.Models.TaskStatus;

namespace TaskGame.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ITaskAssignmentService _assignmentService;

    public TasksController(
        ITaskRepository taskRepository,
        ITaskAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        ITaskAssignmentService assignmentService)
    {
        _taskRepository = taskRepository;
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _assignmentService = assignmentService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    // Chris Ma - Create a new task
    [HttpPost]
    public async Task<ActionResult<TaskDto>> CreateTask(CreateTaskDto createTaskDto)
    {
        var userId = GetCurrentUserId();

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            Title = createTaskDto.Title,
            Description = createTaskDto.Description,
            DueDate = createTaskDto.DueDate,
            Priority = (TaskPriority)createTaskDto.Priority,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.CreateAsync(task);

        return Ok(new TaskDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            Priority = (int)task.Priority
        });
    }

    // Chris Ma - Get tasks created by current user
    [HttpGet("my-created-tasks")]
    public async Task<ActionResult<List<TaskDto>>> GetMyCreatedTasks()
    {
        var userId = GetCurrentUserId();

        var tasks = await _taskRepository.GetByCreatorIdAsync(userId);

        var taskDtos = tasks.Select(t => new TaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            CreatedAt = t.CreatedAt,
            DueDate = t.DueDate,
            Priority = (int)t.Priority
        }).ToList();

        return Ok(taskDtos);
    }

    // Chris Ma - Manually assign task to random user
    [HttpPost("{taskId}/assign")]
    public async Task<ActionResult> AssignTask(Guid taskId)
    {
        var userId = GetCurrentUserId();

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
            return NotFound(new { message = "Task not found" });

        if (task.CreatedByUserId != userId)
            return Forbid();

        var assignment = await _assignmentService.AssignTaskRandomlyAsync(taskId);

        if (assignment == null)
            return BadRequest(new { message = "Unable to assign task. No eligible users or daily limit reached." });

        return Ok(new { message = "Task assigned successfully", assignmentId = assignment.Id });
    }

    // Chris Child - Get tasks assigned to current user
    [HttpGet("my-assignments")]
    public async Task<ActionResult<List<TaskAssignmentDto>>> GetMyAssignments()
    {
        var userId = GetCurrentUserId();

        var assignments = await _assignmentRepository.GetByUserIdAsync(userId);

        var assignmentDtos = assignments.Select(ta => new TaskAssignmentDto
        {
            Id = ta.Id,
            TaskId = ta.TaskId,
            TaskTitle = ta.Task.Title,
            TaskDescription = ta.Task.Description,
            AssignedAt = ta.AssignedAt,
            Status = ta.Status.ToString(),
            DueDate = ta.Task.DueDate,
            Priority = (int)ta.Task.Priority
        }).ToList();

        return Ok(assignmentDtos);
    }

    // Chris Child - Update assignment status
    [HttpPatch("assignments/{assignmentId}/status")]
    public async Task<ActionResult> UpdateAssignmentStatus(Guid assignmentId, [FromBody] string status)
    {
        var userId = GetCurrentUserId();

        var assignment = await _assignmentRepository.GetByIdAsync(assignmentId);

        if (assignment == null || assignment.AssignedToUserId != userId)
            return NotFound(new { message = "Assignment not found" });

        if (Enum.TryParse<TaskStatus>(status, out var taskStatus))
        {
            await _assignmentRepository.UpdateStatusAsync(assignmentId, taskStatus);
            return Ok(new { message = "Status updated successfully" });
        }

        return BadRequest(new { message = "Invalid status" });
    }

    // All users - View all completed tasks (anonymous)
    [HttpGet("completed")]
    public async Task<ActionResult<List<TaskResultDto>>> GetCompletedTasks()
    {
        var completedSubmissions = await _submissionRepository.GetAllCompletedAsync();

        var results = completedSubmissions.Select(ts => new TaskResultDto
        {
            Id = ts.Id,
            TaskTitle = ts.TaskAssignment.Task.Title,
            TaskDescription = ts.TaskAssignment.Task.Description,
            SubmittedAt = ts.SubmittedAt,
            Notes = ts.Notes,
            EventName = ts.TaskAssignment.Task.Event?.Name,
            SubmittedByUsername = ts.SubmittedBy.Username,
            CreatedByUserId = ts.TaskAssignment.Task.CreatedByUserId,
            CreatedByUsername = ts.TaskAssignment.Task.CreatedBy?.Username ?? string.Empty,
            Files = ts.Files.Select(f => new FileDto
            {
                Id = f.Id,
                FileName = f.FileName,
                ContentType = f.ContentType ?? "",
                FileSize = f.FileSize,
                UploadedAt = f.UploadedAt,
                FileUrl = $"/api/submissions/files/{f.Id}"
            }).ToList()
        }).ToList();

        return Ok(results);
    }
}
