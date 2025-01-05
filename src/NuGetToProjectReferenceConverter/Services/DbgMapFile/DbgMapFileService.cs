using Newtonsoft.Json;
using NuGetToProjectReferenceConverter.Services.DbgPath;
using NuGetToProjectReferenceConverter.Services.DbgSolution;
using System;
using System.Collections.Generic;
using System.IO;

namespace NuGetToProjectReferenceConverter.Services.DbgMapFile
{
    public class DbgMapFileService : IDbgMapFileService
    {
        private const string MapFileName = "NuGetToProjectReferenceMap.json";
        private string _mapFilePath;
        private readonly Dictionary<string, string> _map;
        private readonly IDbgSolutionService _dbgSolutionService;
        private readonly IDbgPathService _dbgPathService;

        public DbgMapFileService(IDbgSolutionService dbgSolutionService, IDbgPathService dbgPathService)
        {
            _dbgSolutionService = dbgSolutionService ?? throw new ArgumentNullException(nameof(dbgSolutionService));
            _dbgPathService = dbgPathService ?? throw new ArgumentNullException(nameof(dbgPathService));

            _map = new Dictionary<string, string>();
        }

        private string GetMapFilePath()
        {
            if (_mapFilePath != null)
            {
                return _mapFilePath;
            }

            _mapFilePath = Path.Combine(_dbgSolutionService.GetSolutionDirectory(), MapFileName);
            return _mapFilePath;
        }

        public void LoadOrCreateIfNotExists()
        {
            string directory = Path.GetDirectoryName(GetMapFilePath());
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (File.Exists(GetMapFilePath()))
            {
                Load();
            }
            else
            {
                Save();
            }
        }

        public void Load()
        {
            using (var stream = new StreamReader(GetMapFilePath()))
            {
                var json = stream.ReadToEnd();
                var loadedMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                foreach (var kvp in loadedMap)
                {
                    _map[kvp.Key] = _dbgPathService.ToAbsolutePath(kvp.Value);
                }
            }
        }

        public void Save()
        {
            // Сортировка ключей перед сериализацией
            var sortedMap = new SortedDictionary<string, string>();
            foreach (var kvp in _map)
            {
                sortedMap[kvp.Key] = _dbgPathService.ToRelativePath(kvp.Value);
            }

            using (var stream = new StreamWriter(GetMapFilePath()))
            {
                var json = JsonConvert.SerializeObject(sortedMap, Formatting.Indented);
                stream.Write(json);
            }
        }

        public bool Get(string packageId, out string projectPath)
        {
            return _map.TryGetValue(packageId, out projectPath);
        }

        public void AddOrUpdate(string packageId, string projectPath)
        {
            _map[packageId] = projectPath;
        }
    }
}
