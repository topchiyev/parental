using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Parental.Backend.Models.Entity;
using Parental.Backend.Models.Enum;
using Parental.Backend.Repositories;

namespace Parental.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRoleType.ADMIN))]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly DbRepository _dbRepository;

    public UsersController(ILogger<UsersController> logger, DbRepository dbRepository)
    {
        _logger = logger;
        _dbRepository = dbRepository;
    }

    [HttpGet("list")]
    public IEnumerable<User> GetList()
    {
        var items = _dbRepository.GetEntities<User>(t => t.RoleType != UserRoleType.ADMIN).OrderBy(t => t.RoleType).ThenBy(t => t.Username).ToList();
        return items;
    }

    [HttpPost("create")]
    [Authorize(Roles = nameof(UserRoleType.ADMIN))]
    public ActionResult Create(User item)
    {
        if (item == null)
            return BadRequest("Bad request");

        // Username has at lest 4 characters and at most 20 characters
        if (item.Username.Length < 4 || item.Username.Length > 20)
            return BadRequest("Username must be between 4 and 20 characters");

        // Username contains only english letters, numbers, underscores and dashes
        if (!item.Username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return BadRequest("Username can only contain english letters, numbers, underscores and dashes");

        if (string.IsNullOrEmpty(item.Password))
            return BadRequest("Password is required");

        // Password has at lest 4 characters and at most 20 characters
        if (item.Password.Length < 4 || item.Password.Length > 20)
            return BadRequest("Password must be between 4 and 20 characters");

        // Password contains only english letters, numbers, underscores, dashes
        if (!item.Password.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return BadRequest("Password can only contain english letters, numbers, underscores and dashes");

        if (item.RoleType != UserRoleType.USER)
            return BadRequest("Role must be USER");

        var existingItem = _dbRepository.GetEntities<User>(t => t.Username == item.Username).FirstOrDefault();
        if (existingItem != null)
            return BadRequest("Username is unavailable");

        _dbRepository.AddEntity(item);
        return Ok();
    }

    [HttpPost("update")]
    [Authorize(Roles = nameof(UserRoleType.ADMIN))]
    public ActionResult<User> Update(User item)
    {
        if (item == null)
            return BadRequest("Bad request");

        if (string.IsNullOrEmpty(item.Username))
            return BadRequest("Username is required");

        // Username has at lest 4 characters and at most 20 characters
        if (item.Username.Length < 4 || item.Username.Length > 20)
            return BadRequest("Username must be between 4 and 20 characters");

        // Username contains only english letters, numbers, underscores and dashes
        if (!item.Username.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return BadRequest("Username can only contain english letters, numbers, underscores and dashes");

        if (string.IsNullOrEmpty(item.Password))
            return BadRequest("Password is required");

        // Password has at lest 4 characters and at most 20 characters
        if (item.Password.Length < 4 || item.Password.Length > 20)
            return BadRequest("Password must be between 4 and 20 characters");

        // Password can contain only english letters, numbers, underscores, dashes
        if (!item.Password.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
            return BadRequest("Password can only contain english letters, numbers, underscores and dashes");

        if (item.RoleType != UserRoleType.USER)
            return BadRequest("Role must be USER");

        var existingItem = _dbRepository.GetEntities<User>(t => t.Username == item.Username).FirstOrDefault();
        if (existingItem == null)
            return NotFound("User not found");

        if (item.RoleType != UserRoleType.USER)
            return BadRequest("Role must be USER");

        existingItem.Password = item.Password;
        _dbRepository.UpdateEntity(existingItem);

        return existingItem;
    }

    [HttpDelete("delete")]
    [Authorize(Roles = nameof(UserRoleType.ADMIN))]
    public ActionResult Delete([FromQuery] string username)
    {
        var existingItem = _dbRepository.GetEntities<User>(t => t.Username == username).FirstOrDefault();
        if (existingItem == null)
            return NotFound("User not found");

        var devices = _dbRepository.GetEntities<Device>(d => d.Username == username).ToList();
        foreach (var device in devices)
            _dbRepository.RemoveEntity(device);

        _dbRepository.RemoveEntity(existingItem);
        return Ok();
    }
}