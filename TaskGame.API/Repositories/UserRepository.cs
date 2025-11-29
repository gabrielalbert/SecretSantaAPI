using Dapper;
using TaskGame.API.Data;
using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IDatabaseConnection _dbConnection;

    public UserRepository(IDatabaseConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        const string sql = @"
            SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""CreatedAt"", ""LastLoginAt"", ""IsActive"", ""IsAdmin""
            FROM ""Users""
            WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        const string sql = @"
            SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""CreatedAt"", ""LastLoginAt"", ""IsActive"", ""IsAdmin""
            FROM ""Users""
            WHERE ""Email"" = @Email";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Email = email });
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = @"
            SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""CreatedAt"", ""LastLoginAt"", ""IsActive"", ""IsAdmin""
            FROM ""Users""
            WHERE ""Username"" = @Username";

        using var connection = _dbConnection.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<User>(sql, new { Username = username });
    }

    public async Task<List<User>> GetAllActiveUsersAsync()
    {
        const string sql = @"
            SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""CreatedAt"", ""LastLoginAt"", ""IsActive"", ""IsAdmin""
            FROM ""Users""
            WHERE ""IsActive"" = true
            ORDER BY ""Username""";

        using var connection = _dbConnection.CreateConnection();
        var users = await connection.QueryAsync<User>(sql);
        return users.ToList();
    }

    public async Task<List<User>> GetEligibleUsersForTaskAsync(Guid taskId, Guid creatorId)
    {
        const string sql = @"
            SELECT u.""Id"", u.""Username"", u.""Email"", u.""PasswordHash"", u.""CreatedAt"", u.""LastLoginAt"", u.""IsActive""
            FROM ""Users"" u
            WHERE u.""IsActive"" = true 
                AND u.""Id"" != @CreatorId
                AND u.""Id"" NOT IN (
                    SELECT ""AssignedToUserId""
                    FROM ""TaskAssignments""
                    WHERE ""TaskId"" = @TaskId
                    ORDER BY ""AssignedAt"" DESC
                    LIMIT 1
                )";

        using var connection = _dbConnection.CreateConnection();
        var users = await connection.QueryAsync<User>(sql, new { TaskId = taskId, CreatorId = creatorId });
        return users.ToList();
    }

    public async Task<Guid> CreateAsync(User user)
    {
        const string sql = @"
            INSERT INTO ""Users"" (""Id"", ""Username"", ""Email"", ""PasswordHash"", ""CreatedAt"", ""IsActive"")
            VALUES (@Id, @Username, @Email, @PasswordHash, @CreatedAt, @IsActive)
            RETURNING ""Id""";

        using var connection = _dbConnection.CreateConnection();
        return await connection.ExecuteScalarAsync<Guid>(sql, user);
    }

    public async Task<bool> UpdateLastLoginAsync(Guid userId)
    {
        const string sql = @"
            UPDATE ""Users""
            SET ""LastLoginAt"" = @LastLoginAt
            WHERE ""Id"" = @UserId";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { UserId = userId, LastLoginAt = DateTime.UtcNow });
        return rowsAffected > 0;
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM ""Users""
            WHERE ""Email"" = @Email";

        using var connection = _dbConnection.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email });
        return count > 0;
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM ""Users""
            WHERE ""Username"" = @Username";

        using var connection = _dbConnection.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Username = username });
        return count > 0;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        const string sql = @"
            SELECT ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""CreatedAt"", 
                   ""LastLoginAt"", ""IsActive"", ""IsAdmin""
            FROM ""Users""
            ORDER BY ""CreatedAt"" DESC";

        using var connection = _dbConnection.CreateConnection();
        var result = await connection.QueryAsync<User>(sql);
        return result.ToList();
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        const string sql = @"
            UPDATE ""Users""
            SET ""IsActive"" = @IsActive, ""IsAdmin"" = @IsAdmin
            WHERE ""Id"" = @Id";

        using var connection = _dbConnection.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, user);
        return rowsAffected > 0;
    }
}
