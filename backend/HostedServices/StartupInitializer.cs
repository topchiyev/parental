using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parental.Backend.Models.Entity;
using Parental.Backend.Models.Enum;
using Parental.Backend.Repositories;

namespace Parental.Backend.HostedServices;

public class StartupInitializer : IHostedService
{
    private readonly ILogger<StartupInitializer> _logger;
    private readonly IConfiguration _configuration;
    private readonly DbRepository _dbRepository;


    public StartupInitializer(ILogger<StartupInitializer> logger, IConfiguration configuration, DbRepository dbRepository)
    {
        _logger = logger;
        _configuration = configuration;
        _dbRepository = dbRepository;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        UpdateDefaultUsers();
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private void UpdateDefaultUsers()
    {
        var items = _configuration.GetRequiredSection("DefaultUsers").GetChildren().ToArray();

        foreach (var item in items)
        {
            var username = item.GetValue<string>("Username");
            var password = item.GetValue<string>("Password");
            var roleTypeStr = item.GetValue<string>("RoleType");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                continue;

            if (!Enum.TryParse<UserRoleType>(roleTypeStr, out var roleType))
                continue;

            var user = new User
            {
                Username = username,
                Password = password,
                RoleType = roleType
            };

            var exists = _dbRepository.GetEntities<User>(t => t.Username == username).Any();
            if (!exists)
                _dbRepository.AddEntity(user);
            else
                _dbRepository.UpdateEntity(user);
        }
    }
}
