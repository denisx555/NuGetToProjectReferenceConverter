using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NuGetToProjectReferenceConverter.Tools;

namespace NuGetToProjectReferenceConverter.Services.Indexing
{
    /// <summary>
    /// Сервис для индексирования проектов в файловой системе.
    /// Предоставляет быстрый поиск пути к файлу проекта по его имени.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Назначение:</b>
    /// Данный сервис решает задачу быстрого поиска файла проекта (.csproj) по имени проекта.
    /// Это необходимо при конвертации NuGet-пакетов в ссылки на проекты, когда по идентификатору
    /// пакета (PackageId) нужно найти соответствующий локальный проект.
    /// </para>
    /// <para>
    /// <b>Принцип работы:</b>
    /// <list type="number">
    /// <item>
    /// <description>
    /// При вызове <see cref="BuildIndex"/> рекурсивно сканируется указанная директория
    /// на наличие файлов .csproj.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Для каждого найденного файла извлекается имя проекта (имя файла без расширения)
    /// и сохраняется в словаре: имя проекта -> полный путь к файлу.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// При вызове <see cref="FindProject"/> происходит быстрый поиск по имени в словаре.
    /// </description>
    /// </item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Производительность:</b>
    /// Построение индекса выполняется один раз при инициализации конвертера.
    /// Поиск по индексу имеет сложность O(1), что значительно быстрее
    /// повторного сканирования файловой системы.
    /// </para>
    /// <para>
    /// <b>Важное ограничение - дубликаты имён проектов:</b>
    /// Сервис использует имя проекта как уникальный ключ. Если в индексируемой директории
    /// существует несколько проектов с одинаковым именем (например, "Xafari.Win" в разных папках),
    /// в индекс попадёт только первый найденный проект, а остальные будут проигнорированы.
    /// </para>
    /// <para>
    /// <b>Пример конфликта:</b>
    /// <code>
    /// // Два проекта с одинаковым именем:
    /// // Xafari\Xafari.Win\Xafari.Win.csproj (Library)
    /// // Xafari\Tools\Xafari.Win\Xafari.Win.csproj (WinExe)
    /// // В индекс попадёт только один из них!
    /// </code>
    /// </para>
    /// <para>
    /// <b>Возможные решения проблемы дубликатов:</b>
    /// <list type="bullet">
    /// <item><description>Использовать GUID проекта для различения</description></item>
    /// <item><description>Учитывать OutputType (Library vs WinExe)</description></item>
    /// <item><description>Хранить список всех проектов с одинаковым именем</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Потокобезопасность:</b>
    /// Все публичные методы сервиса потокобезопасны и используют блокировку <see cref="_lock"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// Пример использования:
    /// <code>
    /// var indexService = new ProjectIndexService();
    /// indexService.BuildIndex(@"D:\projects");
    /// 
    /// var projectPath = indexService.FindProject("MyProject");
    /// if (projectPath != null)
    /// {
    ///     Console.WriteLine($"Проект найден: {projectPath}");
    /// }
    /// </code>
    /// </example>
    public class ProjectIndexService : IProjectIndexService
    {
        // Индекс: имя проекта -> полный путь к файлу
        private readonly Dictionary<string, string> _projectIndex;
        
        // Множество проиндексированных директорий
        private readonly HashSet<string> _indexedDirectories;
        
        // Корневая директория индексирования
        private string _rootDirectory;
        
        // Время построения индекса
        private TimeSpan _buildTime;
        
        // Флаг, указывающий, был ли построен индекс
        private bool _isIndexBuilt;
        
        // Объект для синхронизации доступа к индексу
        private readonly object _lock = new object();
        
        /// <summary>
        /// Инициализирует новый экземпляр класса ProjectIndexService
        /// </summary>
        public ProjectIndexService()
        {
            _projectIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _indexedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _isIndexBuilt = false;
        }
        
        /// <summary>
        /// Строит индекс всех проектов в указанной директории
        /// </summary>
        /// <param name="rootDirectory">Корневая директория для индексирования</param>
        public void BuildIndex(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("Корневая директория не может быть пустой", nameof(rootDirectory));
            
            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException($"Директория не найдена: {rootDirectory}");
            
            lock (_lock)
            {
                // Очищаем предыдущий индекс
                ClearIndex();
                
                _rootDirectory = rootDirectory;
                
                var stopwatch = Stopwatch.StartNew();
                
                // FileLogger.Log($"ProjectIndexService: Начало построения индекса в директории: {rootDirectory}");
                
                try
                {
                    // Рекурсивно ищем все .csproj файлы
                    var projectFiles = Directory.GetFiles(
                        rootDirectory, 
                        "*.csproj", 
                        SearchOption.AllDirectories
                    );
                    
                    // FileLogger.Log($"ProjectIndexService: Найдено {projectFiles.Length} файлов .csproj");
                    
                    // Добавляем каждый проект в индекс
                    int duplicateCount = 0;
                    foreach (var projectFile in projectFiles)
                    {
                        var projectName = Path.GetFileNameWithoutExtension(projectFile);
                        
                        // Если проект с таким именем уже есть, логируем дубликат
                        if (_projectIndex.ContainsKey(projectName))
                        {
                            duplicateCount++;
                            // FileLogger.Log($"ProjectIndexService: Дубликат проекта: {projectName} -> {_projectIndex[projectName]} и {projectFile}");
                        }
                        else
                        {
                            _projectIndex[projectName] = projectFile;
                        }
                        
                        // Запоминаем директорию проекта
                        var projectDir = Path.GetDirectoryName(projectFile);
                        _indexedDirectories.Add(projectDir);
                    }
                    
                    if (duplicateCount > 0)
                    {
                        // FileLogger.Log($"ProjectIndexService: Обнаружено {duplicateCount} дубликатов имен проектов");
                    }
                    
                    stopwatch.Stop();
                    _buildTime = stopwatch.Elapsed;
                    _isIndexBuilt = true;
                    
                    // FileLogger.Log($"ProjectIndexService: Индекс построен за {_buildTime.TotalMilliseconds:F2} мс");
                    // FileLogger.Log($"ProjectIndexService: Всего проектов в индексе: {_projectIndex.Count}");
                    // FileLogger.Log($"ProjectIndexService: Проиндексировано директорий: {_indexedDirectories.Count}");
                }
                catch (Exception ex)
                {
                    // FileLogger.Log($"ProjectIndexService: Ошибка при построении индекса: {ex.Message}");
                    ClearIndex();
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Находит путь к проекту по имени
        /// </summary>
        /// <param name="projectName">Имя проекта (без расширения)</param>
        /// <returns>Полный путь к файлу проекта или null, если не найден</returns>
        public string FindProject(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return null;
            
            lock (_lock)
            {
                if (!_isIndexBuilt)
                {
                    // FileLogger.Log($"ProjectIndexService: Попытка поиска до построения индекса");
                    return null;
                }
                
                if (_projectIndex.TryGetValue(projectName, out var projectPath))
                {
                    // FileLogger.Log($"ProjectIndexService: Проект найден в индексе: {projectName} -> {projectPath}");
                    return projectPath;
                }
                
                // FileLogger.Log($"ProjectIndexService: Проект не найден в индексе: {projectName}");
                return null;
            }
        }
        
        /// <summary>
        /// Проверяет, был ли построен индекс
        /// </summary>
        public bool IsIndexBuilt
        {
            get
            {
                lock (_lock)
                {
                    return _isIndexBuilt;
                }
            }
        }
        
        /// <summary>
        /// Очищает индекс
        /// </summary>
        public void ClearIndex()
        {
            lock (_lock)
            {
                _projectIndex.Clear();
                _indexedDirectories.Clear();
                _rootDirectory = null;
                _buildTime = TimeSpan.Zero;
                _isIndexBuilt = false;
                
                // FileLogger.Log("ProjectIndexService: Индекс очищен");
            }
        }
        
        /// <summary>
        /// Получает статистику индекса
        /// </summary>
        /// <returns>Статистика индекса</returns>
        public ProjectIndexStats GetStats()
        {
            lock (_lock)
            {
                return new ProjectIndexStats
                {
                    TotalProjects = _projectIndex.Count,
                    IndexedDirectories = _indexedDirectories.Count,
                    BuildTime = _buildTime,
                    RootDirectory = _rootDirectory
                };
            }
        }
    }
}