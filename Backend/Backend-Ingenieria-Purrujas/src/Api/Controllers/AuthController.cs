using Backend_Ingenieria_Purrujas.Api.Extensions;
using Backend_Ingenieria_Purrujas.Api.Services;
using Backend_Ingenieria_Purrujas.Application.AdminAudit;
using Backend_Ingenieria_Purrujas.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend_Ingenieria_Purrujas.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAdminAuditLogService _adminAuditLogService;
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService, IAdminAuditLogService adminAuditLogService)
    {
        _authService = authService;
        _adminAuditLogService = adminAuditLogService;
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterAdminUserRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            await _adminAuditLogService.RecordForCurrentUserAsync(
                this,
                "Registrar administrador",
                AdminAuditDescriptionBuilder.Created(
                    "Usuario administrador",
                    $"{response.User.FullName} ({response.User.Username})",
                    [
                        AdminAuditDescriptionBuilder.Value("correo", response.User.Email),
                        AdminAuditDescriptionBuilder.Value("rol", response.User.Role)
                    ]),
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            await _adminAuditLogService.RecordAsync(
                response.User.AdminUserId,
                "Inicio de sesion",
                "Inicio sesion en el modulo administrativo.",
                cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await _adminAuditLogService.RecordForCurrentUserAsync(
            this,
            "Cierre de sesion",
            "Cerro sesion en el modulo administrativo.",
            cancellationToken);

        Response.Headers.Append("Clear-Site-Data", "\"cache\", \"storage\", \"cookies\"");
        return NoContent();
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("me")]
    public async Task<ActionResult<AdminUserDto>> Me(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var adminUserId))
        {
            return Unauthorized(new { message = "La sesión no es válida." });
        }

        var user = await _authService.GetProfileAsync(adminUserId, cancellationToken);
        if (user is null)
        {
            return Unauthorized(new { message = "La sesión no corresponde a un administrador activo." });
        }

        return Ok(user);
    }
}
