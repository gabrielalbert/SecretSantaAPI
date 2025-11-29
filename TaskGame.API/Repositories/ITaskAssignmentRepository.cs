using TaskGame.API.Models;
using TaskStatus = TaskGame.API.Models.TaskStatus;

namespace TaskGame.API.Repositories;

public interface ITaskAssignmentRepository
{
    Task<TaskAssignment?> GetByIdAsync(Guid id);
    Task<List<TaskAssignment>> GetByUserIdAsync(Guid userId);
    Task<TaskAssignment?> GetLastAssignmentForTaskAsync(Guid taskId);
    Task<Guid> CreateAsync(TaskAssignment assignment);
    Task<bool> UpdateStatusAsync(Guid assignmentId, TaskStatus status, DateTime? completedAt = null);
    Task<List<TaskAssignment>> GetByTaskIdAsync(Guid taskId);
    Task<bool> DeleteAsync(Guid assignmentId);
    Task<int> DeleteByTaskIdAsync(Guid taskId);
}
