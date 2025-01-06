using Newtonsoft.Json;
using NuGetToProjectReferenceConverter.Services.DbgSolution;
using NuGetToProjectReferenceConverter.Services.Paths;
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
        private readonly IPathService _pathService;

        public DbgMapFileService(IDbgSolutionService dbgSolutionService, IPathService pathService)
        {
            _dbgSolutionService = dbgSolutionService ?? throw new ArgumentNullException(nameof(dbgSolutionService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

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
                    _map[kvp.Key] = kvp.Value != null 
                        ? _pathService.ToAbsolutePath(_dbgSolutionService.GetSolutionDirectory(), kvp.Value) 
                        : null;
                }
            }
        }

        public void Save()
        {
            // Сортировка ключей перед сериализацией
            var sortedMap = new SortedDictionary<string, string>();
            foreach (var kvp in _map)
            {
                sortedMap[kvp.Key] = _pathService.ToRelativePath(_dbgSolutionService.GetSolutionDirectory(), kvp.Value);
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
