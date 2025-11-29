using Microsoft.AspNetCore.Mvc;
using TaskGame.API.DTOs;
using TaskGame.API.Models;
using TaskGame.API.Repositories;
using TaskGame.API.Services;

namespace TaskGame.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public AuthController(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto registerDto)
    {
        // Check if user already exists
        if (await _userRepository.EmailExistsAsync(registerDto.Email))
        {
            return BadRequest(new { message = "Email already registered" });
        }

        if (await _userRepository.UsernameExistsAsync(registerDto.Username))
        {
            return BadRequest(new { message = "Username already taken" });
        }

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _userRepository.CreateAsync(user);

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Username);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Account is inactive" });
        }

        // Update last login
        await _userRepository.UpdateLastLoginAsync(user.Id);

        // Generate JWT token
        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Username);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            IsAdmin = user.IsAdmin
        });
    }
}
