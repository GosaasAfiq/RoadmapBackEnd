using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Persistence;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly DataContext _context;

    public AuthController(IConfiguration config, DataContext context)
    {
        _config = config;
        _context = context;
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse([FromQuery] string credential)
    {
        try
        {
            // Validate Google Token
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential);

            // Check if user exists in the database
            var user = _context.User.FirstOrDefault(u => u.Email == payload.Email);
            if (user == null)
            {
                // If not, create a new user
                user = new Domain.User
                {
                    Id = Guid.NewGuid(),
                    Username = payload.Name,
                    Email = payload.Email,
                    Image = payload.Picture,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.User.Add(user);
                await _context.SaveChangesAsync();
            }

            // Create JWT Token
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                token = token,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = "Invalid token", details = ex.Message });
        }
    }

    private string GenerateJwtToken(Domain.User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"])),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
