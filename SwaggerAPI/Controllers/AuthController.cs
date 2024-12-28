using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Контроллер для управления авторизацией.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Tags("Авторизация")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    
    /// <summary>
    /// Генерация JWT токена.
    /// </summary>
    /// <returns>Сгенерированный токен.</returns>
    /// <response code="200">Токен успешно создан.</response>
    /// <response code="500">Ошибка в настройках JWT.</response>
    [HttpPost("token")]
    public IActionResult GenerateToken()
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var issuer = jwtSettings["Issuer"];
        var audience = jwtSettings["Audience"];
        var secretKey = jwtSettings["SecretKey"];

        if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience) || string.IsNullOrEmpty(secretKey))
        {
            return StatusCode(500, "JWT настройки отсутствуют или неверны");
        }

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            expires: DateTime.UtcNow.AddHours(30),
            signingCredentials: credentials
        );

        return Ok(new { Token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}