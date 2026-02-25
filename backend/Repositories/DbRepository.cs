using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Parental.Backend.Models;

namespace Parental.Backend.Repositories;

public class DbRepository
{
    private static IConfiguration _configuration;
    private static string _dbPath = string.Empty;
    private static string DbPath
    {
        get
        {
            if (string.IsNullOrEmpty(_dbPath))
            {
                var dirPath = _configuration["DataDir"];

                if (string.IsNullOrEmpty(dirPath))
                {
                    var assembly = Assembly.GetExecutingAssembly();;
                    dirPath = Path.GetDirectoryName(assembly.Location);
                }
                
                _dbPath = Path.Combine(dirPath, "db-state.json");
            }

            return _dbPath;
        }
    }

    private static DbState _state = null;
    public DbState State => _state;

    private bool _isSaving = false;
    private bool _isChanged = false;
    private bool _shouldStop = false;
    private Task _saveTask = null;

    public DbRepository(IConfiguration configuration)
    {
        _configuration = configuration;

        LoadState();
        StartSaveStateLoop();
    }

    ~DbRepository()
    {
        StopSaveStateLoop();
    }

    private void LoadState()
    {
        if (!File.Exists(DbPath))
            SaveState();

        string json = File.ReadAllText(DbPath);
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };
        _state = JsonConvert.DeserializeObject<DbState>(json, settings);
    }

    private void SaveState()
    {
        if (_state == null)
            _state = new DbState();

        _state.UpdatedOn = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            Formatting = Formatting.Indented
        };
        string json = JsonConvert.SerializeObject(_state, settings);

        File.WriteAllText(DbPath, json);
    }

    private void StartSaveStateLoop()
    {
        _isSaving = false;
        _isChanged = false;
        _shouldStop = false;

        _saveTask = Task.Run(() =>
        {
            while (true)
            {
                if (_shouldStop)
                    break;

                if (_isSaving || !_isChanged)
                {
                    Task.Delay(200).Wait();
                    continue;
                }

                _isSaving = true;
                SaveState();
                _isChanged = false;
                _isSaving = false;
            }
        });
    }

    private void StopSaveStateLoop()
    {
        _shouldStop = true;
        _saveTask?.Wait();
    }

    public void MarkStateAsChanged()
    {
        _isChanged = true;
    }

    public void AddEntity(IDbEntity entity)
    {
        _state.Entities.Add(entity);
        MarkStateAsChanged();
    }

    public void UpdateEntity(IDbEntity entity)
    {
        MarkStateAsChanged();
    }

    public void RemoveEntity(IDbEntity entity)
    {
        _state.Entities.Remove(entity);
        MarkStateAsChanged();
    }

    public List<T> GetEntities<T>(Func<T, bool> predicate) where T : IDbEntity
    {
        var result = new List<T>();

        foreach (var entity in _state.Entities)
        {
            if (entity is T typedEntity && predicate(typedEntity))
                result.Add(typedEntity);
        }

        return result;
    }
}