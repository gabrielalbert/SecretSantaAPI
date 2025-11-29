using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<List<User>> GetAllActiveUsersAsync();
    Task<List<User>> GetEligibleUsersForTaskAsync(Guid taskId, Guid creatorId);
    Task<Guid> CreateAsync(User user);
    Task<bool> UpdateLastLoginAsync(Guid userId);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<List<User>> GetAllUsersAsync();
    Task<bool> UpdateUserAsync(User user);
}
