using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Parental.Backend.Models.Entity;
using Parental.Backend.Repositories;

namespace Parental.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly ILogger<DevicesController> _logger;
    private readonly DbRepository _dbRepository;

    public DevicesController(ILogger<DevicesController> logger, DbRepository dbRepository)
    {
        _logger = logger;
        _dbRepository = dbRepository;
    }

    [HttpGet("list")]
    public ActionResult<List<Device>> GetList([FromQuery] string username)
    {
        var devices = _dbRepository.GetEntities<Device>(d => true);
        if (!string.IsNullOrEmpty(username))
            devices = devices.Where(d => d.Username == username).ToList();

        return Ok(devices);
    }

    [HttpGet("get")]
    [AllowAnonymous]
    public ActionResult<Device> Get([FromQuery] string id, [FromQuery] string handshake)
    {
        var device = _dbRepository.GetEntities<Device>(d => d.Id == id).FirstOrDefault();
        if (device == null)
            return NotFound("Device not found");

        if (!string.IsNullOrEmpty(handshake) && handshake.ToLower() == "true")
        {
            device.LastHandshakeOn = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            _dbRepository.UpdateEntity(device);
        }

        return Ok(device);
    }

    [HttpPost("update")]
    public ActionResult<string> Update(Device device)
    {
        if (device == null)
            return BadRequest("Bad request");

        if (string.IsNullOrEmpty(device.Username))
            return BadRequest("Username is required");

        if (string.IsNullOrEmpty(device.Name))
            return BadRequest("Device name is required");

        var user = _dbRepository.GetEntities<User>(u => u.Username == device.Username).FirstOrDefault();
        if (user == null)
            return BadRequest("User not found");

        if (string.IsNullOrEmpty(device.Id))
        {
            device.Id = System.Guid.NewGuid().ToString();
            _dbRepository.AddEntity(device);
        }
        else
        {
            var existingDevice = _dbRepository.GetEntities<Device>(d => d.Id == device.Id).FirstOrDefault();
            if (existingDevice == null)
            {
                return NotFound("Device not found");
            }

            existingDevice.Name = device.Name;
            existingDevice.IsManuallyLocked = device.IsManuallyLocked;
            existingDevice.LockedRanges = device.LockedRanges;

            _dbRepository.UpdateEntity(existingDevice);
        }

        return Ok(device.Id);
    }

    [HttpDelete("delete")]
    public ActionResult Delete([FromQuery] string id)
    {
        var existingDevice = _dbRepository.GetEntities<Device>(d => d.Id == id).FirstOrDefault();
        if (existingDevice == null)
        {
            return NotFound("Device not found");
        }

        _dbRepository.RemoveEntity(existingDevice);
        return Ok();
    }
}
