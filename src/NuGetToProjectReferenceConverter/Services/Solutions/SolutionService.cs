using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.Shell;
using NuGetToProjectReferenceConverter.Services.Indexing;
using NuGetToProjectReferenceConverter.Services.Paths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGetToProjectReferenceConverter.Tools;
using static NuGetToProjectReferenceConverter.Tools.PerformanceLogger;

namespace NuGetToProjectReferenceConverter.Services.Solutions
{
    /// <summary>
    /// Provides methods to manage the solution.
    /// Предоставляет методы для управления решением.
    /// </summary>
    public class SolutionService : ISolutionService
    {
        private const string ReplacedProjectsFolderName = "!ReplacedProjects";

        private readonly IServiceProvider _serviceProvider;
        private readonly IPathService _pathService;
        private readonly IProjectIndexService _projectIndexService;
        private ReplacedProjectsFolderItem _replacedProjectsFolder = null;
        private readonly Dictionary<string, string> _projectCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="SolutionService"/> class.
        /// Инициализирует новый экземпляр класса <see cref="SolutionService"/>.
        /// </summary>
        /// <param name="serviceProvider">The service provider. Поставщик услуг.</param>
        /// <param name="pathService">The path service. Сервис путей.</param>
        /// <param name="projectIndexService">The project index service. Сервис индексирования проектов.</param>
        public SolutionService(IServiceProvider serviceProvider, IPathService pathService, IProjectIndexService projectIndexService = null)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _projectIndexService = projectIndexService;
        }

        /// <summary>
        /// Gets all projects in the solution.
        /// Получает все проекты в решении.
        /// </summary>
        /// <returns>A collection of projects. Коллекция проектов.</returns>
        public IEnumerable<EnvDTE.Project> GetAllProjects()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            var solution = dte.Solution;

            foreach (EnvDTE.Project project in solution.Projects)
            {
                foreach (EnvDTE.Project projectResult in GetSubProjects(project))
                {
                    yield return projectResult;
                }
            }
        }

        private IEnumerable<EnvDTE.Project> GetSubProjects(EnvDTE.Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems)
            {
                foreach (EnvDTE.ProjectItem item in project.ProjectItems)
                {
                    if (item.SubProject != null)
                    {
                        foreach (EnvDTE.Project subProject in GetSubProjects(item.SubProject))
                        {
                            yield return subProject;
                        }
                    }
                }
            }
            else
            {
                yield return project;
            }
        }
      
        /// <summary>
        /// Initializes the project index before starting work.
        /// Инициализирует индекс проектов перед началом работы.
        /// </summary>
        /// <param name="rootDirectory">The root directory for indexing. Корневая директория для индексирования.</param>
        public void InitializeProjectIndex(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("Корневая директория не может быть пустой", nameof(rootDirectory));
            
            FileLogger.Log($"SolutionService: Инициализация индекса проектов в директории: {rootDirectory}");
            
            using (var perf = PerformanceLogger.Measure("InitializeProjectIndex"))
            {
                _projectIndexService?.BuildIndex(rootDirectory);
                
                if (_projectIndexService != null && _projectIndexService.IsIndexBuilt)
                {
                    var stats = _projectIndexService.GetStats();
                    FileLogger.Log($"SolutionService: Индекс инициализирован: {stats.TotalProjects} проектов, {stats.IndexedDirectories} директорий, время: {stats.BuildTime.TotalMilliseconds:F2} мс");
                }
            }
        }
      
        /// <summary>
        /// Finds a project path by its name in the solution.
        /// Находит путь к проекту по его имени в решении.
        /// </summary>
        /// <param name="projectName">The project name to search for. Имя проекта для поиска.</param>
        /// <returns>The absolute path to the project, or null if not found. Абсолютный путь к проекту или null, если не найден.</returns>
        private string FindProjectPathByName(string projectName)
        {
        	using (PerformanceLogger.Measure($"FindProjectPathByName({projectName})"))
        	{
        		try
        		{
        			// Сначала проверяем индекс
        			if (_projectIndexService != null && _projectIndexService.IsIndexBuilt)
        			{
        				var indexedPath = _projectIndexService.FindProject(projectName);
        				if (indexedPath != null)
        				{
        					FileLogger.Log($"SolutionService.FindProjectPathByName: Проект найден в индексе: {projectName} -> {indexedPath}");
        					return indexedPath;
        				}
        			}
        			
        			// Если индекс не построен или проект не найден, используем старый метод
        			FileLogger.Log($"SolutionService.FindProjectPathByName: Проект не найден в индексе, поиск в решении: {projectName}");
        			
        			var allProjects = GetAllProjects();
        			foreach (var project in allProjects)
        			{
        				var projName = Path.GetFileNameWithoutExtension(project.FullName);
        				if (projName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
        				{
        					FileLogger.Log($"SolutionService.FindProjectPathByName: Проект найден по имени в решении: {projectName} -> {project.FullName}");
        					return project.FullName;
        				}
        			}
        			FileLogger.Log($"SolutionService.FindProjectPathByName: Проект не найден по имени: {projectName}");
        			return null;
        		}
        		catch (Exception ex)
        		{
        			FileLogger.Log($"SolutionService.FindProjectPathByName: Ошибка при поиске проекта по имени: {projectName}", true);
        			FileLogger.Log(ex);
        			return null;
        		}
        	}
        }
      
        /// <summary>
        /// Finds a project file by name using a combined search strategy with caching.
        /// Находит файл проекта по имени, используя комбинированную стратегию поиска с кэшированием.
        /// Searches in: current project directory, solution directory, and parent directories.
        /// Ищет в: директории текущего проекта, директории решения и родительских директориях.
        /// </summary>
        /// <param name="projectName">The project name to search for. Имя проекта для поиска.</param>
        /// <param name="solutionDir">The solution directory. Директория решения.</param>
        /// <param name="currentProjectDir">The current project directory (optional). Директория текущего проекта (опционально).</param>
        /// <param name="maxParentLevels">Maximum number of parent levels to search. Максимальное количество уровней родительских директорий для поиска.</param>
        /// <returns>The absolute path to the project file, or null if not found. Абсолютный путь к файлу проекта или null, если не найден.</returns>
        private string FindProjectFileInFileSystem(string projectName, string solutionDir, string currentProjectDir = null, int maxParentLevels = 2)
        {
        	using (PerformanceLogger.Measure($"FindProjectFileInFileSystem({projectName})"))
        	{
        		// Проверяем кэш
        		if (_projectCache.TryGetValue(projectName, out var cachedPath))
        		{
        			if (File.Exists(cachedPath))
        			{
        				FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Файл проекта найден в кэше: {projectName} -> {cachedPath}");
        				return cachedPath;
        			}
        			else
        			{
        				// Файл был удален, удаляем из кэша
        				_projectCache.Remove(projectName);
        				FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Файл в кэше не существует, удален из кэша: {projectName}");
        			}
        		}

        		try
        		{
        			// Список директорий для поиска в порядке приоритета
        			var searchDirs = new List<string>();
        			
        			// 1. Директория текущего проекта (если указана)
        			if (!string.IsNullOrEmpty(currentProjectDir) && Directory.Exists(currentProjectDir))
        			{
        				searchDirs.Add(currentProjectDir);
        			}
        			
        			// 2. Директория решения
        			if (Directory.Exists(solutionDir))
        			{
        				searchDirs.Add(solutionDir);
        			}
        			
        			// 3. Родительские директории решения
        			var currentDir = solutionDir;
        			for (int level = 0; level < maxParentLevels; level++)
        			{
        				var parentDir = Directory.GetParent(currentDir);
        				if (parentDir == null)
        				{
        					break;
        				}
        				currentDir = parentDir.FullName;
        				if (Directory.Exists(currentDir) && !searchDirs.Contains(currentDir, StringComparer.OrdinalIgnoreCase))
        				{
        					searchDirs.Add(currentDir);
        				}
        			}
        			
        			// Поиск во всех директориях
        			foreach (var searchDir in searchDirs)
        			{
        				using (PerformanceLogger.Measure($"FindProjectFileInFileSystem.SearchDir({Path.GetFileName(searchDir)})"))
        				{
        					FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Поиск файла проекта: {projectName} в директории: {searchDir}");
        					
        					var csprojFiles = Directory.GetFiles(searchDir, "*.csproj", SearchOption.AllDirectories);
        					FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Найдено {csprojFiles.Length} файлов .csproj");
        					
        					// Логируем первые 5 найденных файлов для диагностики
        					for (int i = 0; i < Math.Min(5, csprojFiles.Length); i++)
        					{
        						FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Найден файл {i + 1}: {csprojFiles[i]}");
        					}
        					
        					foreach (var file in csprojFiles)
        					{
        						var fileName = Path.GetFileNameWithoutExtension(file);
        						if (fileName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
        						{
        							// Добавляем в кэш
        							_projectCache[projectName] = file;
        							FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Файл проекта найден: {projectName} -> {file}");
        							return file;
        						}
        					}
        				}
        			}
        			
        			FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Файл проекта не найден в файловой системе: {projectName}");
        			return null;
        		}
        		catch (Exception ex)
        		{
        			FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Ошибка при поиске файла проекта: {projectName}", true);
        			FileLogger.Log(ex);
        			return null;
        		}
        	}
        }
      
        /// <summary>
        /// Resolves a project path using multiple strategies: absolute path, relative to current project, relative to solution, and search by name.
        /// Разрешает путь к проекту, используя несколько стратегий: абсолютный путь, относительно текущего проекта, относительно решения и поиск по имени.
        /// </summary>
        /// <param name="basePath">The base path (current project directory). Базовый путь (директория текущего проекта).</param>
        /// <param name="relativePath">The relative or absolute path to resolve. Относительный или абсолютный путь для разрешения.</param>
        /// <param name="solutionDir">The solution directory. Директория решения.</param>
        /// <returns>The resolved absolute path to the project. Разрешенный абсолютный путь к проекту.</returns>
        private string ResolveProjectPath(string basePath, string relativePath, string solutionDir)
        {
        	using (PerformanceLogger.Measure($"ResolveProjectPath({Path.GetFileNameWithoutExtension(relativePath)})"))
        	{
        		// 1. Если путь уже абсолютный
        		if (Path.IsPathRooted(relativePath))
        		{
        			FileLogger.Log($"SolutionService.ResolveProjectPath: Путь уже абсолютный: {relativePath}");
        			return relativePath;
        		}
        	
        		// 2. Пробуем относительно текущего проекта
        		var path1 = _pathService.ToAbsolutePath(basePath, relativePath);
        		if (File.Exists(path1))
        		{
        			FileLogger.Log($"SolutionService.ResolveProjectPath: Путь найден относительно текущего проекта: {path1}");
        			return path1;
        		}
        	
        		// 3. Пробуем относительно решения
        		var path2 = _pathService.ToAbsolutePath(solutionDir, relativePath);
        		if (File.Exists(path2))
        		{
        			FileLogger.Log($"SolutionService.ResolveProjectPath: Путь найден относительно решения: {path2}");
        			return path2;
        		}
        	
        		// 4. Пробуем поиск по имени в индексе
        		var projectName = Path.GetFileNameWithoutExtension(relativePath);
        		if (_projectIndexService != null && _projectIndexService.IsIndexBuilt)
        		{
        			var indexedPath = _projectIndexService.FindProject(projectName);
        			if (indexedPath != null && File.Exists(indexedPath))
        			{
        				FileLogger.Log($"SolutionService.ResolveProjectPath: Путь найден в индексе: {projectName} -> {indexedPath}");
        				return indexedPath;
        			}
        		}
        		
        		// 5. Пробуем поиск по имени в решении
        		var foundPath = FindProjectPathByName(projectName);
        		if (foundPath != null && File.Exists(foundPath))
        		{
        			FileLogger.Log($"SolutionService.ResolveProjectPath: Путь найден по имени в решении: {foundPath}");
        			return foundPath;
        		}
        		
        		// 6. Пробуем поиск по имени в файловой системе с кэшированием и комбинированным подходом
        		var foundPathInFs = FindProjectFileInFileSystem(projectName, solutionDir, basePath);
        		if (foundPathInFs != null && File.Exists(foundPathInFs))
        		{
        			FileLogger.Log($"SolutionService.ResolveProjectPath: Путь найден по имени в файловой системе: {foundPathInFs}");
        			return foundPathInFs;
        		}
        		
        		// 7. Возвращаем первый вариант (вызовет исключение, если файл не существует)
        		FileLogger.Log($"SolutionService.ResolveProjectPath: Путь не найден ни одним способом, возвращаем первый вариант: {path1}");
        		return path1;
        	}
        }
      
        private ReplacedProjectsFolderItem GetReplacedProjectsFolder()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_replacedProjectsFolder != null)
            {
                return _replacedProjectsFolder;
            }

            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            var solution = (Solution2)dte.Solution;

            // Find or create the folder for replaced projects
            EnvDTE.Project replacedProjectsFolder = null;
            foreach (EnvDTE.Project project in solution.Projects)
            {
                if (project.Kind == EnvDTE.Constants.vsProjectKindSolutionItems && project.Name == ReplacedProjectsFolderName)
                {
                    replacedProjectsFolder = project;
                    break;
                }
            }

            if (replacedProjectsFolder == null)
            {
                replacedProjectsFolder = solution.AddSolutionFolder(ReplacedProjectsFolderName);
            }

            _replacedProjectsFolder = new ReplacedProjectsFolderItem(replacedProjectsFolder);
            return _replacedProjectsFolder;
        }

		/// <summary>
		/// Adds a project to the current replaced projects folder.
		/// Добавляет проект в текущую папку замененных проектов.
		/// </summary>
		/// <param name="projectPath">The path to the project. Путь к проекту.</param>
		/// <summary>
		/// Adds a project to the current replaced projects folder.
		/// Добавляет проект в текущую папку замененных проектов.
		/// </summary>
		/// <param name="projectPath">The path to the project. Путь к проекту.</param>
		/// <param name="addedList">The list of added projects. Список добавленных проектов.</param>
		public void AddProjectToReplacedProjectsFolder(string projectPath, List<string> addedList)
		{
			using (PerformanceLogger.Measure($"AddProjectToReplacedProjectsFolder({Path.GetFileNameWithoutExtension(projectPath)})"))
			{
				FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Добавление проекта: {projectPath}");

				try
				{
					ThreadHelper.ThrowIfNotOnUIThread();

					if (addedList is null)
					{
						FileLogger.Log("SolutionService.AddProjectToReplacedProjectsFolder: addedList равен null, выбрасываем исключение");
						throw new ArgumentNullException(nameof(addedList));
					}

					if (!File.Exists(projectPath))
					{
						throw new FileNotFoundException($"Файл проекта не существует: {projectPath}");
					}

					var replacedProjectsFolder = GetReplacedProjectsFolder();
					var projectName = Path.GetFileNameWithoutExtension(projectPath);
					FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Имя проекта: {projectName}");

					// Check if the project already exists in the folder
					foreach (EnvDTE.ProjectItem item in replacedProjectsFolder.ProjectItems)
					{
						if (item.Name == projectName)
						{
							FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Проект {projectName} уже существует в папке, пропускаем");
							return;
						}
					}

					FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Добавление файла проекта в папку");

					replacedProjectsFolder.AddFromFile(projectPath);
					addedList.Add(projectPath);
					FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Проект добавлен в папку и в список");

					using (var projectCollection = new ProjectCollection())
					{
						using (PerformanceLogger.Measure($"AddProjectToReplacedProjectsFolder.LoadMSBuild({projectName})"))
						{
							FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Загрузка MSBuild проекта: {projectPath}");
							var subProject = projectCollection.LoadProject(projectPath);
							var items = subProject.GetItems("ProjectReference").ToArray();
							var subProjectPath = Path.GetDirectoryName(subProject.FullPath);
							FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Найдено {items.Length} ссылок на проекты в {projectPath}");

							foreach (var item in items)
							{
								FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Обработка ссылки на проект: {item.EvaluatedInclude}");

								// Используем комбинированный подход для разрешения пути к проекту
								var solutionDir = GetSolutionDirectory();
								string subProjectAbsolutePath = ResolveProjectPath(subProjectPath, item.EvaluatedInclude, solutionDir);

								AddProjectToReplacedProjectsFolder(subProjectAbsolutePath, addedList);
							}
						}
					}
				}
				catch (Exception ex)
				{
					FileLogger.Log(ex);
					throw;
				}
			}
		}

		/// <summary>
		/// Gets the directory of the solution.
		/// Получает каталог решения.
		/// </summary>
		/// <returns>The solution directory. Каталог решения.</returns>
		public string GetSolutionDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = (EnvDTE.DTE)_serviceProvider.GetService(typeof(EnvDTE.DTE));
            return Path.GetDirectoryName(dte.Solution.FullName);
        }
    }
}
