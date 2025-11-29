using TaskGame.API.Models;
using TaskGame.API.Repositories;
using TaskStatus = TaskGame.API.Models.TaskStatus;

namespace TaskGame.API.Services;

public interface ITaskAssignmentService
{
    Task<TaskAssignment?> AssignTaskRandomlyAsync(Guid taskId);
    Task<List<User>> GetEligibleUsersForTaskAsync(Guid taskId, Guid creatorId);
}

public class TaskAssignmentService : ITaskAssignmentService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITaskAssignmentRepository _assignmentRepository;
    private readonly Random _random = new();

    public TaskAssignmentService(
        ITaskRepository taskRepository,
        IUserRepository userRepository,
        ITaskAssignmentRepository assignmentRepository)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _assignmentRepository = assignmentRepository;
    }

    public async Task<TaskAssignment?> AssignTaskRandomlyAsync(Guid taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
            return null;

        // Get eligible users (excluding the task creator)
        var eligibleUsers = await GetEligibleUsersForTaskAsync(taskId, task.CreatedByUserId);

        if (!eligibleUsers.Any())
            return null;

        // Check how many assignments were made today
        var todayAssignmentsCount = await _taskRepository.GetTodayAssignmentCountAsync(taskId);

        if (todayAssignmentsCount >= task.MaxDailyAssignments)
            return null;

        // Randomly select a user
        var selectedUser = eligibleUsers[_random.Next(eligibleUsers.Count)];

        // Create assignment
        var assignment = new TaskAssignment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            AssignedToUserId = selectedUser.Id,
            AssignedAt = DateTime.UtcNow,
            Status = TaskStatus.Pending
        };

        await _assignmentRepository.CreateAsync(assignment);
        return assignment;
    }

    public async Task<List<User>> GetEligibleUsersForTaskAsync(Guid taskId, Guid creatorId)
    {
        return await _userRepository.GetEligibleUsersForTaskAsync(taskId, creatorId);
    }
}
