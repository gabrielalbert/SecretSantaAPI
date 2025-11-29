using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskGame.API.DTOs;
using TaskGame.API.Models;
using TaskGame.API.Repositories;

namespace TaskGame.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InvitationsController : ControllerBase
{
    private readonly IEventInvitationRepository _invitationRepository;

    public InvitationsController(IEventInvitationRepository invitationRepository)
    {
        _invitationRepository = invitationRepository;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    // Get all invitations for current user
    [HttpGet("my-invitations")]
    public async Task<ActionResult<List<EventInvitationDto>>> GetMyInvitations()
    {
        var userId = GetCurrentUserId();
        var invitations = await _invitationRepository.GetByUserIdAsync(userId);

        var invitationDtos = invitations.Select(inv => new EventInvitationDto
        {
            Id = inv.Id,
            EventId = inv.EventId,
            EventName = inv.Event?.Name ?? "",
            UserId = inv.UserId,
            Username = "", // Current user, no need to show
            ChrisMaUsername = inv.ChrisMaUser?.Username ?? "",
            ChrisChildUsername = inv.ChrisChildUser?.Username ?? "",
            InvitedAt = inv.InvitedAt,
            Status = inv.Status.ToString(),
            ResponseAt = inv.ResponseAt
        }).ToList();

        return Ok(invitationDtos);
    }

    // Get pending invitations for current user
    [HttpGet("my-invitations/pending")]
    public async Task<ActionResult<List<EventInvitationDto>>> GetMyPendingInvitations()
    {
        var userId = GetCurrentUserId();
        var invitations = await _invitationRepository.GetPendingInvitationsByUserIdAsync(userId);

        var invitationDtos = invitations.Select(inv => new EventInvitationDto
        {
            Id = inv.Id,
            EventId = inv.EventId,
            EventName = inv.Event?.Name ?? "",
            UserId = inv.UserId,
            Username = "",
            ChrisMaUsername = inv.ChrisMaUser?.Username ?? "",
            ChrisChildUsername = inv.ChrisChildUser?.Username ?? "",
            InvitedAt = inv.InvitedAt,
            Status = inv.Status.ToString(),
            ResponseAt = inv.ResponseAt
        }).ToList();

        return Ok(invitationDtos);
    }

    // Get specific invitation details
    [HttpGet("{invitationId}")]
    public async Task<ActionResult<EventInvitationDto>> GetInvitation(Guid invitationId)
    {
        var userId = GetCurrentUserId();
        var invitation = await _invitationRepository.GetByIdAsync(invitationId);

        if (invitation == null)
            return NotFound(new { message = "Invitation not found" });

        if (invitation.UserId != userId)
            return Forbid();

        return Ok(new EventInvitationDto
        {
            Id = invitation.Id,
            EventId = invitation.EventId,
            EventName = invitation.Event?.Name ?? "",
            UserId = invitation.UserId,
            Username = invitation.User?.Username ?? "",
            ChrisMaUsername = invitation.ChrisMaUser?.Username ?? "",
            ChrisChildUsername = invitation.ChrisChildUser?.Username ?? "",
            InvitedAt = invitation.InvitedAt,
            Status = invitation.Status.ToString(),
            ResponseAt = invitation.ResponseAt
        });
    }

    // Respond to invitation (accept or decline)
    [HttpPost("{invitationId}/respond")]
    public async Task<ActionResult> RespondToInvitation(Guid invitationId, [FromBody] InvitationResponseDto response)
    {
        var userId = GetCurrentUserId();
        var invitation = await _invitationRepository.GetByIdAsync(invitationId);

        if (invitation == null)
            return NotFound(new { message = "Invitation not found" });

        if (invitation.UserId != userId)
            return Forbid();

        if (invitation.Status != InvitationStatus.Pending)
            return BadRequest(new { message = "Invitation has already been responded to" });

        var newStatus = response.Accept ? InvitationStatus.Accepted : InvitationStatus.Declined;
        await _invitationRepository.UpdateStatusAsync(invitationId, newStatus, DateTime.UtcNow);

        return Ok(new { 
            message = $"Invitation {(response.Accept ? "accepted" : "declined")} successfully",
            status = newStatus.ToString()
        });
    }
}
