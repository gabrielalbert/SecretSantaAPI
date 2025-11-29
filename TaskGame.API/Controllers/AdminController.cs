using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskGame.API.DTOs;
using TaskGame.API.Models;
using TaskGame.API.Repositories;

namespace TaskGame.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IEventInvitationRepository _invitationRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;

    public AdminController(
        IUserRepository userRepository,
        IEventRepository eventRepository,
        IEventInvitationRepository invitationRepository,
        ITaskRepository taskRepository,
        ITaskAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository)
    {
        _userRepository = userRepository;
        _eventRepository = eventRepository;
        _invitationRepository = invitationRepository;
        _taskRepository = taskRepository;
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    private async Task<bool> IsAdminAsync()
    {
        var userId = GetCurrentUserId();
        var user = await _userRepository.GetByIdAsync(userId);
        return user?.IsAdmin ?? false;
    }

    // ============================================
    // User Management
    // ============================================

    [HttpGet("users")]
    public async Task<ActionResult<List<UserDto>>> GetAllUsers()
    {
        if (!await IsAdminAsync())
            return Forbid();

        var users = await _userRepository.GetAllUsersAsync();
        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt,
            IsActive = u.IsActive,
            IsAdmin = u.IsAdmin
        }).ToList();

        return Ok(userDtos);
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<UserDto>> GetUser(Guid userId)
    {
        if (!await IsAdminAsync())
            return Forbid();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            IsAdmin = user.IsAdmin
        });
    }

    [HttpPatch("users/{userId}")]
    public async Task<ActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserDto updateDto)
    {
        if (!await IsAdminAsync())
            return Forbid();

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found" });

        // Update only provided fields
        if (updateDto.IsActive.HasValue)
            user.IsActive = updateDto.IsActive.Value;

        if (updateDto.IsAdmin.HasValue)
            user.IsAdmin = updateDto.IsAdmin.Value;

        await _userRepository.UpdateUserAsync(user);

        return Ok(new { message = "User updated successfully" });
    }

    // ============================================
    // Event Management
    // ============================================

    [HttpGet("events")]
    public async Task<ActionResult<List<EventDto>>> GetAllEvents()
    {
        if (!await IsAdminAsync())
            return Forbid();

        var events = await _eventRepository.GetAllAsync();
        var eventDtos = new List<EventDto>();

        foreach (var evt in events)
        {
            var invitedCount = (await _invitationRepository.GetByEventIdAsync(evt.Id)).Count;
            var acceptedCount = await _invitationRepository.GetAcceptedCountByEventIdAsync(evt.Id);

            eventDtos.Add(new EventDto
            {
                Id = evt.Id,
                Name = evt.Name,
                Description = evt.Description,
                StartDate = evt.StartDate,
                EndDate = evt.EndDate,
                CreatedAt = evt.CreatedAt,
                IsActive = evt.IsActive,
                MaxTasksPerUser = evt.MaxTasksPerUser,
                InvitedCount = invitedCount,
                AcceptedCount = acceptedCount
            });
        }

        return Ok(eventDtos);
    }

    [HttpGet("events/{eventId}")]
    public async Task<ActionResult<EventDetailDto>> GetEvent(Guid eventId)
    {
        if (!await IsAdminAsync())
            return Forbid();

        var evt = await _eventRepository.GetByIdWithDetailsAsync(eventId);
        if (evt == null)
            return NotFound(new { message = "Event not found" });

        var taskCount = await _eventRepository.GetTaskCountByEventIdAsync(eventId);

        var invitationDtos = evt.Invitations.Select(inv => new EventInvitationDto
        {
            Id = inv.Id,
            EventId = inv.EventId,
            EventName = evt.Name,
            UserId = inv.UserId,
            Username = inv.User?.Username ?? "",
            ChrisMaUsername = inv.ChrisMaUser?.Username ?? "",
            ChrisChildUsername = inv.ChrisChildUser?.Username ?? "",
            InvitedAt = inv.InvitedAt,
            Status = inv.Status.ToString(),
            ResponseAt = inv.ResponseAt
        }).ToList();

        return Ok(new EventDetailDto
        {
            Id = evt.Id,
            Name = evt.Name,
            Description = evt.Description,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            CreatedAt = evt.CreatedAt,
            IsActive = evt.IsActive,
            MaxTasksPerUser = evt.MaxTasksPerUser,
            Invitations = invitationDtos,
            TaskCount = taskCount
        });
    }

    [HttpPost("events")]
    public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventDto createDto)
    {
        if (!await IsAdminAsync())
            return Forbid();

        if (createDto.UserIds.Count < 2)
            return BadRequest(new { message = "At least 2 users are required for an event" });

        if (createDto.EndDate <= createDto.StartDate)
            return BadRequest(new { message = "End date must be after start date" });

        var userId = GetCurrentUserId();

        // Create event
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Name = createDto.Name,
            Description = createDto.Description,
            StartDate = createDto.StartDate,
            EndDate = createDto.EndDate,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            MaxTasksPerUser = createDto.MaxTasksPerUser
        };

        await _eventRepository.CreateAsync(evt);

        // Create random role mappings for each user
        var random = new Random();
        var userIdsList = createDto.UserIds.ToList();

        foreach (var invitedUserId in userIdsList)
        {
            // Randomly select Chris Ma and Chris Child from other users
            var otherUsers = userIdsList.Where(id => id != invitedUserId).ToList();
            
            if (otherUsers.Count == 0)
                continue;

            var chrisMaUserId = otherUsers[random.Next(otherUsers.Count)];
            
            // For Chris Child, try to pick someone different from Chris Ma if possible
            var childCandidates = otherUsers.Where(id => id != chrisMaUserId).ToList();
            var chrisChildUserId = childCandidates.Any() 
                ? childCandidates[random.Next(childCandidates.Count)]
                : otherUsers[random.Next(otherUsers.Count)];

            var invitation = new EventInvitation
            {
                Id = Guid.NewGuid(),
                EventId = evt.Id,
                UserId = invitedUserId,
                ChrisMaUserId = chrisMaUserId,
                ChrisChildUserId = chrisChildUserId,
                InvitedAt = DateTime.UtcNow,
                Status = InvitationStatus.Pending
            };

            await _invitationRepository.CreateAsync(invitation);
        }

        return Ok(new EventDto
        {
            Id = evt.Id,
            Name = evt.Name,
            Description = evt.Description,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            CreatedAt = evt.CreatedAt,
            IsActive = evt.IsActive,
            MaxTasksPerUser = evt.MaxTasksPerUser,
            InvitedCount = userIdsList.Count,
            AcceptedCount = 0
        });
    }

    [HttpPut("events/{eventId}")]
    public async Task<ActionResult<EventDto>> UpdateEvent(Guid eventId, [FromBody] UpdateEventDto updateDto)
    {
        if (!await IsAdminAsync())
            return Forbid();

        var evt = await _eventRepository.GetByIdAsync(eventId);
        if (evt == null)
            return NotFound(new { message = "Event not found" });

        if (string.IsNullOrWhiteSpace(updateDto.Name))
            return BadRequest(new { message = "Event name is required" });

        if (updateDto.MaxTasksPerUser < 1)
            return BadRequest(new { message = "Max tasks per user must be at least 1" });

        if (updateDto.EndDate <= updateDto.StartDate)
            return BadRequest(new { message = "End date must be after start date" });

        evt.Name = updateDto.Name;
        evt.Description = updateDto.Description;
        evt.StartDate = updateDto.StartDate;
        evt.EndDate = updateDto.EndDate;
        evt.MaxTasksPerUser = updateDto.MaxTasksPerUser;
        evt.IsActive = updateDto.IsActive;

        await _eventRepository.UpdateAsync(evt);

        var invitations = await _invitationRepository.GetByEventIdAsync(evt.Id);
        var acceptedCount = await _invitationRepository.GetAcceptedCountByEventIdAsync(evt.Id);

        return Ok(new EventDto
        {
            Id = evt.Id,
            Name = evt.Name,
            Description = evt.Description,
            StartDate = evt.StartDate,
            EndDate = evt.EndDate,
            CreatedAt = evt.CreatedAt,
            IsActive = evt.IsActive,
            MaxTasksPerUser = evt.MaxTasksPerUser,
            InvitedCount = invitations.Count,
            AcceptedCount = acceptedCount
        });
    }

    [HttpPatch("events/{eventId}/status")]
    public async Task<ActionResult> UpdateEventStatus(Guid eventId, [FromBody] bool isActive)
    {
        if (!await IsAdminAsync())
            return Forbid();

        var evt = await _eventRepository.GetByIdAsync(eventId);
        if (evt == null)
            return NotFound(new { message = "Event not found" });

        evt.IsActive = isActive;
        await _eventRepository.UpdateAsync(evt);

        return Ok(new { message = "Event status updated successfully" });
    }

    [HttpDelete("events/{eventId}")]
    public async Task<ActionResult> DeleteEvent(Guid eventId)
    {
        if (!await IsAdminAsync())
            return Forbid();

        var evt = await _eventRepository.GetByIdAsync(eventId);
        if (evt == null)
            return NotFound(new { message = "Event not found" });

        // Load tasks tied to this event and clean up related data
        var eventTasks = await _taskRepository.GetByEventIdAsync(eventId);

        foreach (var task in eventTasks)
        {
            var assignments = await _assignmentRepository.GetByTaskIdAsync(task.Id);

            foreach (var assignment in assignments)
            {
                var submission = await _submissionRepository.GetByAssignmentIdWithFilesAsync(assignment.Id);

                if (submission != null)
                {
                    await DeleteSubmissionWithFilesAsync(submission);
                }

                await _assignmentRepository.DeleteAsync(assignment.Id);
            }

            await _taskRepository.DeleteAsync(task.Id);
        }

        // Remove invitations associated with the event
        await _invitationRepository.DeleteByEventIdAsync(eventId);

        await _eventRepository.DeleteAsync(eventId);

        return Ok(new { message = "Event and related tasks deleted successfully" });
    }

    private async Task DeleteSubmissionWithFilesAsync(TaskSubmission submission)
    {
        if (submission.Files?.Any() != true)
        {
            // Ensure database cleanup even if Files was null/empty in incoming model
            await _submissionRepository.DeleteFilesBySubmissionIdAsync(submission.Id);
            await _submissionRepository.DeleteAsync(submission.Id);
            return;
        }

        foreach (var file in submission.Files)
        {
            TryDeleteFile(file.FilePath);
        }

        await _submissionRepository.DeleteFilesBySubmissionIdAsync(submission.Id);
        await _submissionRepository.DeleteAsync(submission.Id);
    }

    private static void TryDeleteFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;

        if (!System.IO.File.Exists(filePath))
            return;

        try
        {
            System.IO.File.Delete(filePath);
        }
        catch (IOException)
        {
            // Ignore failures to delete files from disk; database entries are removed separately
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore lack of permissions; best-effort clean-up
        }
    }
}
