using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event?> GetByIdWithDetailsAsync(Guid id);
    Task<List<Event>> GetAllAsync();
    Task<List<Event>> GetActiveEventsAsync();
    Task<Guid> CreateAsync(Event eventItem);
    Task<bool> UpdateAsync(Event eventItem);
    Task<bool> DeleteAsync(Guid id);
    Task<int> GetTaskCountByEventIdAsync(Guid eventId);
}
