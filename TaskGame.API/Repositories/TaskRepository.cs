using Dapper;
using TaskGame.API.Data;
using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly IDatabaseConnection _dbConnection;

    public TaskRepository(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT ""Id"", ""Title"", ""Description"", ""CreatedAt"", ""DueDate"", 
                   ""CreatedByUserId"", ""Priority"", ""MaxDailyAssignments"", ""EventId""
            FROM ""Tasks""            
            WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<TaskItem>(sql, new { Id = id });
    }

    public async Task<List<TaskItem>> GetByCreatorIdAsync(Guid creatorId)
    {
        const string sql = @"
            SELECT ""Id"", ""Title"", ""Description"", ""CreatedAt"", ""DueDate"", 
                   ""CreatedByUserId"", ""Priority"", ""MaxDailyAssignments"", ""EventId""
            FROM ""Tasks""
            WHERE ""CreatedByUserId"" = @CreatorId
            ORDER BY ""CreatedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var tasks = await connection.QueryAsync<TaskItem>(sql, new { CreatorId = creatorId });
        return tasks.ToList();
    }

    public async Task<Guid> CreateAsync(TaskItem task)
    {
        const string sql = @"
            INSERT INTO ""Tasks"" (""Id"", ""Title"", ""Description"", ""CreatedAt"", ""DueDate"", 
                                  ""CreatedByUserId"", ""Priority"", ""MaxDailyAssignments"", ""EventId"")
            VALUES (@Id, @Title, @Description, @CreatedAt, @DueDate, 
                    @CreatedByUserId, @Priority, @MaxDailyAssignments,@EventId)
            RETURNING ""Id""";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<Guid>(sql, task);
    }

    public async Task<int> GetTodayAssignmentCountAsync(Guid taskId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM ""TaskAssignments""
            WHERE ""TaskId"" = @TaskId
                AND DATE(""AssignedAt"") = @Today";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { TaskId = taskId, Today = DateTime.UtcNow.Date });
    }

    public async Task<List<TaskItem>> GetByEventIdAsync(Guid eventId)
    {
        const string sql = @"
            SELECT ""Id"", ""Title"", ""Description"", ""CreatedAt"", ""DueDate"", 
                   ""CreatedByUserId"", ""Priority"", ""MaxDailyAssignments"", ""EventId""
            FROM ""Tasks""
            WHERE ""EventId"" = @EventId";        

        using var connection = _dbConnection.CreateConnection();
        var tasks = await connection.QueryAsync<TaskItem>(sql, new { EventId = eventId });
        return tasks.ToList();
    }

    public async Task<bool> DeleteAsync(Guid taskId)
    {
        const string sql = @"DELETE FROM ""Tasks"" WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = taskId });
        return rowsAffected > 0;
    }
}
