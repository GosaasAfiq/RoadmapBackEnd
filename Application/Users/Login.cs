using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Dto;

namespace Application.Users
{
    public class Login
    {
        public class Command : IRequest<LoginResponse>
        {
            public string Credential { get; set; }  // The Google credential passed from the front end
        }

        public class Handler : IRequestHandler<Command, LoginResponse>
        {
            private readonly DataContext _context;
            private readonly IConfiguration _config;

            public Handler(DataContext context, IConfiguration config)
            {
                _context = context;
                _config = config;
            }

            public async Task<LoginResponse> Handle(Command request, CancellationToken cancellationToken)
            {

                try
                {
                    // Validate Google Token
                    var payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential);

                    // Check if user exists in the database
                    var user = await _context.User.FirstOrDefaultAsync(u => u.Email == payload.Email, cancellationToken);
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
                        await _context.SaveChangesAsync(cancellationToken);

                    }
                    

                    // Generate JWT Token
                    var token = GenerateJwtToken(user);

                    return new LoginResponse
                    {
                        Id = user.Id,
                        Username = user.Username,
                        Email = user.Email,
                        Image = user.Image,
                        CreatedAt = user.CreatedAt,
                        UpdatedAt = user.UpdatedAt,
                        Token = token
                    };
                }
                catch (Exception ex)
                {
                    throw new Exception("Invalid token", ex); // You can handle this error appropriately
                }
            }

            private string GenerateJwtToken(Domain.User user)
            {
                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()) // Include the UserId

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
    }
}
