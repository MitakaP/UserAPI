using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAPI.DTOs;
using UserAPI.Interfaces;
using System.Security.Claims;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Tags("Auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;

    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    // POST: api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserResponseDto>> Register(
        CreateUserDto dto)
    {
        var user = await _userService.CreateAsync(dto);

        if (user == null)
            return Conflict("A user with this email already exists.");

        return CreatedAtAction(
            nameof(UsersController.GetUser),
            "Users",
            new { id = user.Id },
            user);
    }

    // POST: api/auth/login
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(
        LoginDto dto)
    {
        var response = await _userService.LoginAsync(dto);

        if (response == null)
            return Unauthorized("Invalid email or password.");

        return Ok(response);
    }

    // POST: api/auth/verify-mfa-login
    [HttpPost("verify-mfa-login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> VerifyMfaLogin(
        LoginMfaDto dto)
    {
        var response = await _userService.VerifyMfaLoginAsync(dto);

        if (response == null)
            return Unauthorized("Invalid email, password or MFA code.");

        return Ok(response);
    }

    // POST: api/auth/forgot-password
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(
        ForgotPasswordDto dto)
    {
        await _userService.ForgotPasswordAsync(dto);

        return Ok(new
        {
            message = "If an account with this email exists, a reset email has been sent."
        });
    }

    // POST: api/auth/reset-password
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordDto dto)
    {
        var result = await _userService.ResetPasswordAsync(dto);

        if (!result)
            return BadRequest("Invalid or expired password reset token.");

        return Ok(new
        {
            message = "Your password has been successfully reset."
        });
    }

    // POST: api/auth/setup-mfa
    [HttpPost("setup-mfa")]
    public async Task<IActionResult> SetupMfa()
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _userService.SetupMfaAsync(userId);

        if (result == null)
            return Conflict("MFA is already enabled or user was not found.");

        return Ok(result);
    }

    // POST: api/auth/verify-mfa-setup
    [HttpPost("verify-mfa-setup")]
    public async Task<IActionResult> VerifyMfaSetup(VerifyMfaDto dto)
    {
        var userIdClaim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _userService.VerifyMfaSetupAsync(
            userId,
            dto);

        if (!result)
            return BadRequest("Invalid code or expired MFA setup.");

        return Ok(new
        {
            message = "MFA has been successfully enabled."
        });
    }

}
