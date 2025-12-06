using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public interface ISubmissionRepository
{
    Task<TaskSubmission?> GetByIdAsync(Guid id);
    Task<TaskSubmission?> GetByAssignmentIdAsync(Guid assignmentId);
    Task<List<TaskSubmission>> GetAllCompletedAsync(Guid userId);
    Task<Guid> CreateAsync(TaskSubmission submission);
    Task<Guid> CreateFileAsync(SubmissionFile file);
    Task<SubmissionFile?> GetFileByIdAsync(Guid fileId);
    Task<TaskSubmission?> GetByAssignmentIdWithFilesAsync(Guid assignmentId);
    Task<List<SubmissionFile>> GetFilesBySubmissionIdAsync(Guid submissionId);
    Task<int> DeleteFilesBySubmissionIdAsync(Guid submissionId);
    Task<bool> DeleteAsync(Guid submissionId);
}
