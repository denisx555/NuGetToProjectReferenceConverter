using Newtonsoft.Json;
using NuGetToProjectReferenceConverter.Services.Paths;
using NuGetToProjectReferenceConverter.Services.Solutions;
using System;
using System.Collections.Generic;
using System.IO;

namespace NuGetToProjectReferenceConverter.Services.MapFile
{
    /// <summary>
    /// Provides methods to manage the map file.
    /// Предоставляет методы для управления файлом карты.
    /// </summary>
    public class MapFileService : IMapFileService
    {
        private const string MapFileName = "NuGetToProjectReferenceMap.json";
        private string _mapFilePath;
        private readonly Dictionary<string, string> _map;
        private readonly ISolutionService _solutionService;
        private readonly IPathService _pathService;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapFileService"/> class.
        /// Инициализирует новый экземпляр класса <see cref="MapFileService"/>.
        /// </summary>
        /// <param name="solutionService">The solution service. Сервис решения.</param>
        /// <param name="pathService">The path service. Сервис путей.</param>
        public MapFileService(ISolutionService solutionService, IPathService pathService)
        {
            _solutionService = solutionService ?? throw new ArgumentNullException(nameof(solutionService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));

            _map = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the path to the map file.
        /// Получает путь к файлу карты.
        /// </summary>
        /// <returns>The path to the map file. Путь к файлу карты.</returns>
        private string GetMapFilePath()
        {
            if (_mapFilePath != null)
            {
                return _mapFilePath;
            }

            _mapFilePath = Path.Combine(_solutionService.GetSolutionDirectory(), MapFileName);
            return _mapFilePath;
        }

        /// <summary>
        /// Loads the map file or creates it if it does not exist.
        /// Загружает файл карты или создает его, если он не существует.
        /// </summary>
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

        /// <summary>
        /// Loads the map file.
        /// Загружает файл карты.
        /// </summary>
        public void Load()
        {
            using (var stream = new StreamReader(GetMapFilePath()))
            {
                var json = stream.ReadToEnd();
                var loadedMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                _map.Clear();

                foreach (var kvp in loadedMap)
                {
                    _map[kvp.Key] = kvp.Value != null 
                        ? _pathService.ToAbsolutePath(_solutionService.GetSolutionDirectory(), kvp.Value) 
                        : null;
                }
            }
        }

        /// <summary>
        /// Saves the map file.
        /// Сохраняет файл карты.
        /// </summary>
        public void Save()
        {
            // Sort keys before serialization
            var sortedMap = new SortedDictionary<string, string>();
            foreach (var kvp in _map)
            {
                sortedMap[kvp.Key] = _pathService.ToRelativePath(_solutionService.GetSolutionDirectory(), kvp.Value);
            }

            using (var stream = new StreamWriter(GetMapFilePath()))
            {
                var json = JsonConvert.SerializeObject(sortedMap, Formatting.Indented);
                stream.Write(json);
            }
        }

        /// <summary>
        /// Gets a project path by package ID.
        /// Получает путь к проекту по идентификатору пакета.
        /// </summary>
        /// <param name="packageId">The package ID. Идентификатор пакета.</param>
        /// <param name="projectPath">The project path. Путь к проекту.</param>
        /// <returns>True if the package ID exists, otherwise false. Возвращает true, если идентификатор пакета существует, иначе false.</returns>
        public bool Get(string packageId, out string projectPath)
        {
            return _map.TryGetValue(packageId, out projectPath);
        }

        /// <summary>
        /// Adds or updates a mapping entry.
        /// Добавляет или обновляет запись в карте.
        /// </summary>
        /// <param name="packageId">The package ID. Идентификатор пакета.</param>
        /// <param name="projectPath">The project path. Путь к проекту.</param>
        public void AddOrUpdate(string packageId, string projectPath)
        {
            _map[packageId] = projectPath;
        }
    }
}