using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Parental.Backend.Models.Entity;
using Parental.Backend.Repositories;

namespace Parental.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    public class LogInRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    private readonly ILogger<AuthenticationController> _logger;
    private readonly DbRepository _dbRepository;
    private readonly SymmetricSecurityKey _key;

    public AuthenticationController(ILogger<AuthenticationController> logger, DbRepository dbRepository)
    {
        _logger = logger;
        _dbRepository = dbRepository;

        string jwtTokenKey = string.Empty;
        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream("Parental.Backend.jwt-token.key"))
        {
            if (stream != null)
            {
                using (var reader = new StreamReader(stream))
                {
                    jwtTokenKey = reader.ReadToEnd();
                    if (string.IsNullOrEmpty(jwtTokenKey))
                    {
                        _logger.LogError("JWT token key not found.");
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(jwtTokenKey))
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtTokenKey));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<string> LogIn(LogInRequest request)
    {
        if (_key == null)
            return StatusCode(500, "JWT token key not configured");

        if (request == null || string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Username and password are required");

        var user = _dbRepository.GetEntities<User>(u => u.Username == request.Username && u.Password == request.Password).FirstOrDefault();
        if (user == null)
            return Unauthorized("Invalid username or password");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.RoleType.ToString()),
        };

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDesc = new SecurityTokenDescriptor
        {
            SigningCredentials = creds,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.Now.AddDays(10)
        };

        var tokenHandler = new JwtSecurityTokenHandler();

        var token = tokenHandler.CreateToken(tokenDesc);

        var tokenStr = tokenHandler.WriteToken(token);

        return Ok(tokenStr);
    }
}