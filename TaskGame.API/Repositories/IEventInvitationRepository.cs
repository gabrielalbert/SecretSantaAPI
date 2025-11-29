using TaskGame.API.Models;

namespace TaskGame.API.Repositories;

public interface IEventInvitationRepository
{
    Task<EventInvitation?> GetByIdAsync(Guid id);
    Task<List<EventInvitation>> GetByEventIdAsync(Guid eventId);
    Task<List<EventInvitation>> GetByUserIdAsync(Guid userId);
    Task<EventInvitation?> GetByEventAndUserAsync(Guid eventId, Guid userId);
    Task<Guid> CreateAsync(EventInvitation invitation);
    Task<bool> UpdateStatusAsync(Guid id, InvitationStatus status, DateTime responseAt);
    Task<bool> DeleteAsync(Guid id);
    Task<List<EventInvitation>> GetPendingInvitationsByUserIdAsync(Guid userId);
    Task<int> GetAcceptedCountByEventIdAsync(Guid eventId);
    Task<int> DeleteByEventIdAsync(Guid eventId);
}
