using Dapper;
using TaskGame.API.Data;
using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public class EventRepository : IEventRepository
{
    private readonly IDatabaseConnection _dbConnection;

    public EventRepository(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Description"", ""StartDate"", ""EndDate"", 
                   ""CreatedByUserId"", ""CreatedAt"", ""IsActive"", ""MaxTasksPerUser""
            FROM ""Events""
            WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Event>(sql, new { Id = id });
    }

    public async Task<Event?> GetByIdWithDetailsAsync(Guid id)
    {
        const string sql = @"
            SELECT e.""Id"", e.""Name"", e.""Description"", e.""StartDate"", e.""EndDate"", 
                   e.""CreatedByUserId"", e.""CreatedAt"", e.""IsActive"", e.""MaxTasksPerUser"",
                   ei.""Id"", ei.""EventId"", ei.""UserId"", ei.""ChrisMaUserId"", ei.""ChrisChildUserId"",
                   ei.""InvitedAt"", ei.""Status"", ei.""ResponseAt"",
                   u.""Id"", u.""Username"", u.""Email""
            FROM ""Events"" e
            LEFT JOIN ""EventInvitations"" ei ON e.""Id"" = ei.""EventId""
            LEFT JOIN ""Users"" u ON ei.""UserId"" = u.""Id""
            WHERE e.""Id"" = @Id
            ORDER BY ei.""InvitedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var eventDict = new Dictionary<Guid, Event>();

        var events = await connection.QueryAsync<Event, EventInvitation, User, Event>(
            sql,
            (eventItem, invitation, user) =>
            {
                if (!eventDict.TryGetValue(eventItem.Id, out var currentEvent))
                {
                    currentEvent = eventItem;
                    currentEvent.Invitations = new List<EventInvitation>();
                    eventDict.Add(eventItem.Id, currentEvent);
                }

                if (invitation != null && user != null)
                {
                    invitation.User = user;
                    currentEvent.Invitations.Add(invitation);
                }

                return currentEvent;
            },
            new { Id = id },
            splitOn: "Id,Id"
        );

        return eventDict.Values.FirstOrDefault();
    }

    public async Task<List<Event>> GetAllAsync()
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Description"", ""StartDate"", ""EndDate"", 
                   ""CreatedByUserId"", ""CreatedAt"", ""IsActive"", ""MaxTasksPerUser""
            FROM ""Events""
            ORDER BY ""CreatedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var result = await connection.QueryAsync<Event>(sql);
        return result.ToList();
    }

    public async Task<List<Event>> GetActiveEventsAsync()
    {
        const string sql = @"
            SELECT ""Id"", ""Name"", ""Description"", ""StartDate"", ""EndDate"", 
                   ""CreatedByUserId"", ""CreatedAt"", ""IsActive"", ""MaxTasksPerUser""
            FROM ""Events""
            WHERE ""IsActive"" = TRUE 
              AND ""StartDate"" <= @Now 
              AND ""EndDate"" >= @Now
            ORDER BY ""StartDate"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var result = await connection.QueryAsync<Event>(sql, new { Now = DateTime.UtcNow });
        return result.ToList();
    }

    public async Task<Guid> CreateAsync(Event eventItem)
    {
        const string sql = @"
            INSERT INTO ""Events"" (""Id"", ""Name"", ""Description"", ""StartDate"", ""EndDate"", 
                                  ""CreatedByUserId"", ""CreatedAt"", ""IsActive"", ""MaxTasksPerUser"")
            VALUES (@Id, @Name, @Description, @StartDate, @EndDate, 
                    @CreatedByUserId, @CreatedAt, @IsActive, @MaxTasksPerUser)
            RETURNING ""Id""";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<Guid>(sql, eventItem);
    }

    public async Task<bool> UpdateAsync(Event eventItem)
    {
        const string sql = @"
            UPDATE ""Events""
            SET ""Name"" = @Name, 
                ""Description"" = @Description, 
                ""StartDate"" = @StartDate, 
                ""EndDate"" = @EndDate,
                ""IsActive"" = @IsActive, 
                ""MaxTasksPerUser"" = @MaxTasksPerUser
            WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, eventItem);
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM ""Events"" WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<int> GetTaskCountByEventIdAsync(Guid eventId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM ""Tasks""
            WHERE ""EventId"" = @EventId";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { EventId = eventId });
    }
}
