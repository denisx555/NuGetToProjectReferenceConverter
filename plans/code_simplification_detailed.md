# Детализированный план упрощения кода

## 1. SolutionService.cs

### 1.1. Удалить using-директивы (строки 4-5)

**Удалить:**
```csharp
// Строка 4
using NuGetToProjectReferenceConverter.Services.Indexing;
// Строка 5
using NuGetToProjectReferenceConverter.Services.MapFile;
```

### 1.2. Удалить поля (строки 26-29)

**Удалить:**
```csharp
// Строки 26-29
private readonly IProjectIndexService _projectIndexService;
private IMapFileService _mapFileService;
private ReplacedProjectsFolderItem _replacedProjectsFolder = null;
private readonly Dictionary<string, string> _projectCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
```

**Заменить на:**
```csharp
// Только это поле остаётся
private ReplacedProjectsFolderItem _replacedProjectsFolder = null;
```

### 1.3. Упростить конструктор (строки 31-46)

**Было (строки 39-46):**
```csharp
public SolutionService(IServiceProvider serviceProvider, IPathService pathService, 
    IProjectIndexService projectIndexService = null, IMapFileService mapFileService = null)
{
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
    _projectIndexService = projectIndexService;
    _mapFileService = mapFileService;
}
```

**Станет:**
```csharp
public SolutionService(IServiceProvider serviceProvider, IPathService pathService)
{
    _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
}
```

### 1.4. Удалить метод SetMapFileService (строки 48-56)

**Удалить полностью:**
```csharp
/// <summary>
/// Sets the map file service after construction to resolve circular dependency.
/// Устанавливает сервис файла карты после создания для разрешения циклической зависимости.
/// </summary>
/// <param name="mapFileService">The map file service. Сервис файла карты соответствий.</param>
public void SetMapFileService(IMapFileService mapFileService)
{
    _mapFileService = mapFileService;
}
```

### 1.5. Удалить метод InitializeProjectIndex (строки 101-123)

**Удалить полностью:**
```csharp
/// <summary>
/// Initializes the project index before starting work.
/// Инициализирует индекс проектов перед началом работы.
/// </summary>
/// <param name="rootDirectory">The root directory for indexing. Корневая директория для индексирования.</param>
public void InitializeProjectIndex(string rootDirectory)
{
    if (string.IsNullOrWhiteSpace(rootDirectory))
        throw new ArgumentException("Корневая директория не может быть пустой", nameof(rootDirectory));
    
    // FileLogger.Log($"SolutionService: Инициализация индекса проектов в директории: {rootDirectory}");
    
    using (var perf = PerformanceLogger.Measure("InitializeProjectIndex"))
    {
        _projectIndexService?.BuildIndex(rootDirectory);
        
        if (_projectIndexService != null && _projectIndexService.IsIndexBuilt)
        {
            var stats = _projectIndexService.GetStats();
            // FileLogger.Log($"SolutionService: Индекс инициализирован: {stats.TotalProjects} проектов, {stats.IndexedDirectories} директорий, время: {stats.BuildTime.TotalMilliseconds:F2} мс");
        }
    }
}
```

### 1.6. Удалить метод FindProjectPathByName (строки 125-171)

**Удалить полностью** (47 строк)

### 1.7. Удалить метод FindProjectFileInFileSystem (строки 173-277)

**Удалить полностью** (104 строки)

### 1.8. Удалить метод ResolveProjectPath (строки 279-379)

**Удалить полностью** (100 строк)

### 1.9. Изменить метод AddProjectToReplacedProjectsFolder (строки 424-493)

**Было (строки 475-483):**
```csharp
foreach (var item in items)
{
    // FileLogger.Log($"SolutionService.AddProjectToReplacedProjectsFolder: Обработка ссылки на проект: {item.EvaluatedInclude}");

    // Используем комбинированный подход для разрешения пути к проекту
    var solutionDir = GetSolutionDirectory();
    string subProjectAbsolutePath = ResolveProjectPath(subProjectPath, item.EvaluatedInclude, solutionDir);

    AddProjectToReplacedProjectsFolder(subProjectAbsolutePath, addedList);
}
```

**Станет:**
```csharp
foreach (var item in items)
{
    // Путь уже должен быть абсолютным
    string subProjectAbsolutePath = item.EvaluatedInclude;
    
    if (!File.Exists(subProjectAbsolutePath))
    {
        FileLogger.Log($"[AddProjectToReplacedProjectsFolder] Файл проекта не существует: {subProjectAbsolutePath}", true);
        continue;
    }

    AddProjectToReplacedProjectsFolder(subProjectAbsolutePath, addedList);
}
```

---

## 2. ISolutionService.cs

### 2.1. Удалить метод InitializeProjectIndex (строки 25-30)

**Удалить:**
```csharp
/// <summary>
/// Initializes the project index before starting work.
/// Инициализирует индекс проектов перед началом работы.
/// </summary>
/// <param name="rootDirectory">The root directory for indexing. Корневая директория для индексирования.</param>
void InitializeProjectIndex(string rootDirectory);
```

---

## 3. Command.cs

### 3.1. Удалить using-директиву (строка 4)

**Удалить:**
```csharp
using NuGetToProjectReferenceConverter.Services.Indexing;
```

### 3.2. Изменить метод Execute (строки 117-137)

**Было:**
```csharp
// Создаем сервисы
IPathService pathService = new PathService();
IProjectIndexService projectIndexService = new ProjectIndexService();
SolutionService solutionService = new SolutionService((IServiceProvider)ServiceProvider, pathService, projectIndexService);
IMapFileService mapFileService = new MapFileService(solutionService, pathService);

// Устанавливаем mapFileService в solutionService для разрешения циклической зависимости
solutionService.SetMapFileService(mapFileService);
FileLogger.Log("[Command] mapFileService установлен в solutionService");

// Инициализируем индекс проектов
// Используем директорию analysis (два уровня вверх от решения) как корневую для индексирования
var rootDirectory = Path.GetDirectoryName(Path.GetDirectoryName(solutionDir));
solutionService.InitializeProjectIndex(rootDirectory);

// Создаем конвертер и выполняем конвертацию
var replaceNuGetWithProjectReference = new NuGetToProjectReferenceConverter(solutionService,
    mapFileService,
    pathService);
```

**Станет:**
```csharp
// Создаем сервисы
IPathService pathService = new PathService();
ISolutionService solutionService = new SolutionService((IServiceProvider)ServiceProvider, pathService);
IMapFileService mapFileService = new MapFileService(solutionService, pathService);

// Создаем конвертер и выполняем конвертацию
var replaceNuGetWithProjectReference = new NuGetToProjectReferenceConverter(solutionService,
    mapFileService,
    pathService);
```

---

## 4. Удалить файлы

После успешной компиляции удалить:
- `src/NuGetToProjectReferenceConverter/Services/Indexing/IProjectIndexService.cs`
- `src/NuGetToProjectReferenceConverter/Services/Indexing/ProjectIndexService.cs`

---

## Итоговая статистика

| Файл | Удалено строк | Комментарий |
|------|---------------|-------------|
| SolutionService.cs | ~280 | Поля, методы, using |
| ISolutionService.cs | ~6 | Метод интерфейса |
| Command.cs | ~12 | Создание сервисов |
| **Всего** | **~300** | |

---

## Порядок выполнения

1. Изменить `SolutionService.cs` (удалить всё лишнее)
2. Изменить `ISolutionService.cs` (удалить метод)
3. Изменить `Command.cs` (упростить создание сервисов)
4. Проверить компиляцию
5. Удалить файлы `IProjectIndexService.cs` и `ProjectIndexService.cs`
