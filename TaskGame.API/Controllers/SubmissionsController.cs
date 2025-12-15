using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskGame.API.DTOs;
using TaskGame.API.Models;
using TaskGame.API.Repositories;
using TaskStatus = TaskGame.API.Models.TaskStatus;

namespace TaskGame.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubmissionsController : ControllerBase
{
    private readonly ITaskAssignmentRepository _assignmentRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public SubmissionsController(
        ITaskAssignmentRepository assignmentRepository,
        ISubmissionRepository submissionRepository,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _assignmentRepository = assignmentRepository;
        _submissionRepository = submissionRepository;
        _configuration = configuration;
        _environment = environment;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB
    [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
    // Submit a task with files
    [HttpPost]
    public async Task<ActionResult> SubmitTask([FromForm] Guid taskAssignmentId, [FromForm] string? notes, [FromForm] List<IFormFile>? files)
    {
        var userId = GetCurrentUserId();

        var assignment = await _assignmentRepository.GetByIdAsync(taskAssignmentId);

        if (assignment == null || assignment.AssignedToUserId != userId)
            return NotFound(new { message = "Assignment not found" });

        var existingSubmission = await _submissionRepository.GetByAssignmentIdAsync(taskAssignmentId);
        if (existingSubmission != null)
            return BadRequest(new { message = "Task already submitted" });

        // Create submission
        var submission = new TaskSubmission
        {
            Id = Guid.NewGuid(),
            TaskAssignmentId = taskAssignmentId,
            SubmittedByUserId = userId,
            Notes = notes,
            SubmittedAt = DateTime.UtcNow,
            Files = new List<SubmissionFile>()
        };

        // Handle file uploads
        if (files != null && files.Any())
        {
            var uploadPath = _configuration["FileUpload:UploadPath"] ?? "uploads";
            var fullUploadPath = Path.Combine(_environment.ContentRootPath, uploadPath);

            if (!Directory.Exists(fullUploadPath))
            {
                Directory.CreateDirectory(fullUploadPath);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                    var filePath = Path.Combine(fullUploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var submissionFile = new SubmissionFile
                    {
                        Id = Guid.NewGuid(),
                        TaskSubmissionId = submission.Id,
                        FileName = file.FileName,
                        FilePath = filePath,
                        ContentType = file.ContentType,
                        FileSize = file.Length,
                        UploadedAt = DateTime.UtcNow
                    };

                    submission.Files.Add(submissionFile);
                }
            }
        }

        await _submissionRepository.CreateAsync(submission);

        if (submission.Files.Any())
        {
            foreach (var submissionFile in submission.Files)
            {
                await _submissionRepository.CreateFileAsync(submissionFile);
            }
        }

        // Update assignment status
        await _assignmentRepository.UpdateStatusAsync(taskAssignmentId, TaskStatus.Completed, DateTime.UtcNow);

        return Ok(new { message = "Task submitted successfully", submissionId = submission.Id });
    }

    // Download a file
    [HttpGet("files/{fileId}")]
    public async Task<ActionResult> DownloadFile(Guid fileId)
    {
        var file = await _submissionRepository.GetFileByIdAsync(fileId);

        if (file == null)
            return NotFound(new { message = "File not found" });

        Console.WriteLine($"Attempting to access file at path: {file.FilePath}");
        if (!System.IO.File.Exists(file.FilePath))
            return NotFound(new { message = "Physical file not found" });

        var memory = new MemoryStream();
        using (var stream = new FileStream(file.FilePath, FileMode.Open, FileAccess.Read))
        {
            await stream.CopyToAsync(memory);
        }
        memory.Position = 0;

        return File(memory, file.ContentType ?? "application/octet-stream", file.FileName);
    }

    // Get submission details
    [HttpGet("{submissionId}")]
    public async Task<ActionResult<TaskResultDto>> GetSubmission(Guid submissionId)
    {
        var submission = await _submissionRepository.GetByIdAsync(submissionId);

        if (submission == null)
            return NotFound(new { message = "Submission not found" });

        var result = new TaskResultDto
        {
            Id = submission.Id,
            TaskTitle = submission.TaskAssignment.Task.Title,
            TaskDescription = submission.TaskAssignment.Task.Description,
            SubmittedAt = submission.SubmittedAt,
            Notes = submission.Notes,
            Files = submission.Files.Select(f => new FileDto
            {
                Id = f.Id,
                FileName = f.FileName,
                ContentType = f.ContentType ?? "",
                FileSize = f.FileSize,
                UploadedAt = f.UploadedAt
            }).ToList()
        };

        return Ok(result);
    }
}
