using Dapper;
using TaskGame.API.Data;
using TaskGame.API.Models;
using TaskStatus = TaskGame.API.Models.TaskStatus;

namespace TaskGame.API.Repositories;

public class TaskAssignmentRepository : ITaskAssignmentRepository
{
    private readonly IDatabaseConnection _dbConnection;

    public TaskAssignmentRepository(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<TaskAssignment?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT ta.""Id"", ta.""TaskId"", ta.""AssignedToUserId"", ta.""AssignedAt"", 
                   ta.""Status"", ta.""CompletedAt"",
                   t.""Id"", t.""Title"", t.""Description"", t.""CreatedAt"", t.""DueDate"",
                   t.""CreatedByUserId"", t.""Priority"", t.""MaxDailyAssignments""
            FROM ""TaskAssignments"" ta
            INNER JOIN ""Tasks"" t ON ta.""TaskId"" = t.""Id""
            WHERE ta.""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var assignments = await connection.QueryAsync<TaskAssignment, TaskItem, TaskAssignment>(
            sql,
            (assignment, task) =>
            {
                assignment.Task = task;
                return assignment;
            },
            new { Id = id },
            splitOn: "Id"
        );

        return assignments.FirstOrDefault();
    }

    public async Task<List<TaskAssignment>> GetByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT ta.""Id"", ta.""TaskId"", ta.""AssignedToUserId"", ta.""AssignedAt"", 
                   ta.""Status"", ta.""CompletedAt"",
                   t.""Id"", t.""Title"", t.""Description"", t.""CreatedAt"", t.""DueDate"",
                   t.""CreatedByUserId"", t.""Priority"", t.""MaxDailyAssignments"", t.""EventId""
            FROM ""TaskAssignments"" ta
            INNER JOIN ""Tasks"" t ON ta.""TaskId"" = t.""Id""            
            WHERE ta.""AssignedToUserId"" = @UserId
            ORDER BY ta.""AssignedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var assignments = await connection.QueryAsync<TaskAssignment, TaskItem, TaskAssignment>(
            sql,
            (assignment, task) =>
            {
                assignment.Task = task;                
                return assignment;
            },
            new { UserId = userId },
            splitOn: "Id"
        );

        return assignments.ToList();
    }

    public async Task<List<TaskAssignment>> GetByTaskIdAsync(Guid taskId)
    {
        const string sql = @"
            SELECT ta.""Id"", ta.""TaskId"", ta.""AssignedToUserId"", ta.""AssignedAt"", 
                   ta.""Status"", ta.""CompletedAt"",
                   t.""Id"", t.""Title"", t.""Description"", t.""CreatedAt"", t.""DueDate"",
                   t.""CreatedByUserId"", t.""Priority"", t.""MaxDailyAssignments"", t.""EventId""
            FROM ""TaskAssignments"" ta
            INNER JOIN ""Tasks"" t ON ta.""TaskId"" = t.""Id""
            WHERE ta.""TaskId"" = @TaskId
            ORDER BY ta.""AssignedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var assignments = await connection.QueryAsync<TaskAssignment, TaskItem, TaskAssignment>(
            sql,
            (assignment, task) =>
            {
                assignment.Task = task;
                return assignment;
            },
            new { TaskId = taskId },
            splitOn: "Id"
        );

        return assignments.ToList();
    }

    public async Task<TaskAssignment?> GetLastAssignmentForTaskAsync(Guid taskId)
    {
        const string sql = @"
            SELECT ""Id"", ""TaskId"", ""AssignedToUserId"", ""AssignedAt"", ""Status"", ""CompletedAt""
            FROM ""TaskAssignments""
            WHERE ""TaskId"" = @TaskId
            ORDER BY ""AssignedAt"" DESC
            LIMIT 1";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TaskAssignment>(sql, new { TaskId = taskId });
    }

    public async Task<Guid> CreateAsync(TaskAssignment assignment)
    {
        const string sql = @"
            INSERT INTO ""TaskAssignments"" (""Id"", ""TaskId"", ""AssignedToUserId"", ""AssignedAt"", ""Status"")
            VALUES (@Id, @TaskId, @AssignedToUserId, @AssignedAt, @Status)
            RETURNING ""Id""";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<Guid>(sql, assignment);
    }

    public async Task<bool> UpdateStatusAsync(Guid assignmentId, TaskStatus status, DateTime? completedAt = null)
    {
        const string sql = @"
            UPDATE ""TaskAssignments""
            SET ""Status"" = @Status, ""CompletedAt"" = @CompletedAt
            WHERE ""Id"" = @AssignmentId";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { AssignmentId = assignmentId, Status = (int)status, CompletedAt = completedAt });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(Guid assignmentId)
    {
        const string sql = @"DELETE FROM ""TaskAssignments"" WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = assignmentId });
        return rowsAffected > 0;
    }

    public async Task<int> DeleteByTaskIdAsync(Guid taskId)
    {
        const string sql = @"DELETE FROM ""TaskAssignments"" WHERE ""TaskId"" = @TaskId";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteAsync(sql, new { TaskId = taskId });
    }
}
