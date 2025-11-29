using Dapper;
using TaskGame.API.Data;
using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public class EventInvitationRepository : IEventInvitationRepository
{
    private readonly IDatabaseConnection _dbConnection;

    public EventInvitationRepository(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<EventInvitation?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT ei.""Id"", ei.""EventId"", ei.""UserId"", ei.""ChrisMaUserId"", ei.""ChrisChildUserId"",
                   ei.""InvitedAt"", ei.""Status"", ei.""ResponseAt"",
                   e.""Id"", e.""Name"", e.""Description"", e.""StartDate"", e.""EndDate"",
                   u.""Id"", u.""Username"", u.""Email"",
                   ma.""Id"", ma.""Username"",
                   child.""Id"", child.""Username""
            FROM ""EventInvitations"" ei
            INNER JOIN ""Events"" e ON ei.""EventId"" = e.""Id""
            INNER JOIN ""Users"" u ON ei.""UserId"" = u.""Id""
            INNER JOIN ""Users"" ma ON ei.""ChrisMaUserId"" = ma.""Id""
            INNER JOIN ""Users"" child ON ei.""ChrisChildUserId"" = child.""Id""
            WHERE ei.""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var invitations = await connection.QueryAsync<EventInvitation, Event, User, User, User, EventInvitation>(
            sql,
            (invitation, eventItem, user, maUser, childUser) =>
            {
                invitation.Event = eventItem;
                invitation.User = user;
                invitation.ChrisMaUser = maUser;
                invitation.ChrisChildUser = childUser;
                return invitation;
            },
            new { Id = id },
            splitOn: "Id,Id,Id,Id"
        );

        return invitations.FirstOrDefault();
    }

    public async Task<List<EventInvitation>> GetByEventIdAsync(Guid eventId)
    {
        const string sql = @"
            SELECT ei.""Id"", ei.""EventId"", ei.""UserId"", ei.""ChrisMaUserId"", ei.""ChrisChildUserId"",
                   ei.""InvitedAt"", ei.""Status"", ei.""ResponseAt"",
                   u.""Id"", u.""Username"", u.""Email"",
                   ma.""Id"", ma.""Username"",
                   child.""Id"", child.""Username""
            FROM ""EventInvitations"" ei
            INNER JOIN ""Users"" u ON ei.""UserId"" = u.""Id""
            INNER JOIN ""Users"" ma ON ei.""ChrisMaUserId"" = ma.""Id""
            INNER JOIN ""Users"" child ON ei.""ChrisChildUserId"" = child.""Id""
            WHERE ei.""EventId"" = @EventId
            ORDER BY ei.""InvitedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var invitations = await connection.QueryAsync<EventInvitation, User, User, User, EventInvitation>(
            sql,
            (invitation, user, maUser, childUser) =>
            {
                invitation.User = user;
                invitation.ChrisMaUser = maUser;
                invitation.ChrisChildUser = childUser;
                return invitation;
            },
            new { EventId = eventId },
            splitOn: "Id,Id,Id"
        );

        return invitations.ToList();
    }

    public async Task<List<EventInvitation>> GetByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT ei.""Id"", ei.""EventId"", ei.""UserId"", ei.""ChrisMaUserId"", ei.""ChrisChildUserId"",
                   ei.""InvitedAt"", ei.""Status"", ei.""ResponseAt"",
                   e.""Id"", e.""Name"", e.""Description"", e.""StartDate"", e.""EndDate"",
                   ma.""Id"", ma.""Username"",
                   child.""Id"", child.""Username""
            FROM ""EventInvitations"" ei
            INNER JOIN ""Events"" e ON ei.""EventId"" = e.""Id""
            INNER JOIN ""Users"" ma ON ei.""ChrisMaUserId"" = ma.""Id""
            INNER JOIN ""Users"" child ON ei.""ChrisChildUserId"" = child.""Id""
            WHERE ei.""UserId"" = @UserId
            ORDER BY ei.""InvitedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var invitations = await connection.QueryAsync<EventInvitation, Event, User, User, EventInvitation>(
            sql,
            (invitation, eventItem, maUser, childUser) =>
            {
                invitation.Event = eventItem;
                invitation.ChrisMaUser = maUser;
                invitation.ChrisChildUser = childUser;
                return invitation;
            },
            new { UserId = userId },
            splitOn: "Id,Id,Id"
        );

        return invitations.ToList();
    }

    public async Task<EventInvitation?> GetByEventAndUserAsync(Guid eventId, Guid userId)
    {
        const string sql = @"
            SELECT ""Id"", ""EventId"", ""UserId"", ""ChrisMaUserId"", ""ChrisChildUserId"",
                   ""InvitedAt"", ""Status"", ""ResponseAt""
            FROM ""EventInvitations""
            WHERE ""EventId"" = @EventId AND ""UserId"" = @UserId";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<EventInvitation>(sql, new { EventId = eventId, UserId = userId });
    }

    public async Task<Guid> CreateAsync(EventInvitation invitation)
    {
        const string sql = @"
            INSERT INTO ""EventInvitations"" (""Id"", ""EventId"", ""UserId"", ""ChrisMaUserId"", ""ChrisChildUserId"",
                                            ""InvitedAt"", ""Status"")
            VALUES (@Id, @EventId, @UserId, @ChrisMaUserId, @ChrisChildUserId, @InvitedAt, @Status)
            RETURNING ""Id""";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<Guid>(sql, invitation);
    }

    public async Task<bool> UpdateStatusAsync(Guid id, InvitationStatus status, DateTime responseAt)
    {
        const string sql = @"
            UPDATE ""EventInvitations""
            SET ""Status"" = @Status, ""ResponseAt"" = @ResponseAt
            WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id, Status = (int)status, ResponseAt = responseAt });
        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        const string sql = @"DELETE FROM ""EventInvitations"" WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { Id = id });
        return rowsAffected > 0;
    }

    public async Task<List<EventInvitation>> GetPendingInvitationsByUserIdAsync(Guid userId)
    {
        const string sql = @"
            SELECT ei.""Id"", ei.""EventId"", ei.""UserId"", ei.""ChrisMaUserId"", ei.""ChrisChildUserId"",
                   ei.""InvitedAt"", ei.""Status"", ei.""ResponseAt"",
                   e.""Id"", e.""Name"", e.""Description"", e.""StartDate"", e.""EndDate"",
                   ma.""Id"", ma.""Username"",
                   child.""Id"", child.""Username""
            FROM ""EventInvitations"" ei
            INNER JOIN ""Events"" e ON ei.""EventId"" = e.""Id""
            INNER JOIN ""Users"" ma ON ei.""ChrisMaUserId"" = ma.""Id""
            INNER JOIN ""Users"" child ON ei.""ChrisChildUserId"" = child.""Id""
            WHERE ei.""UserId"" = @UserId AND ei.""Status"" = 1
            ORDER BY ei.""InvitedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var invitations = await connection.QueryAsync<EventInvitation, Event, User, User, EventInvitation>(
            sql,
            (invitation, eventItem, maUser, childUser) =>
            {
                invitation.Event = eventItem;
                invitation.ChrisMaUser = maUser;
                invitation.ChrisChildUser = childUser;
                return invitation;
            },
            new { UserId = userId },
            splitOn: "Id,Id,Id"
        );

        return invitations.ToList();
    }

    public async Task<int> GetAcceptedCountByEventIdAsync(Guid eventId)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM ""EventInvitations""
            WHERE ""EventId"" = @EventId AND ""Status"" = 2";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(sql, new { EventId = eventId });
    }

    public async Task<int> DeleteByEventIdAsync(Guid eventId)
    {
        const string sql = @"DELETE FROM ""EventInvitations"" WHERE ""EventId"" = @EventId";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteAsync(sql, new { EventId = eventId });
    }
}
