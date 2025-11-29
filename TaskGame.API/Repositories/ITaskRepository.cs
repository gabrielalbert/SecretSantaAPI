using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id);
    Task<List<TaskItem>> GetByCreatorIdAsync(Guid creatorId);
    Task<Guid> CreateAsync(TaskItem task);
    Task<int> GetTodayAssignmentCountAsync(Guid taskId);
    Task<List<TaskItem>> GetByEventIdAsync(Guid eventId);
    Task<bool> DeleteAsync(Guid taskId);
}
