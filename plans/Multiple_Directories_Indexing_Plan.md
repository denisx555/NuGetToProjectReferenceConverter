# План: Индексирование нескольких директорий

## Проблема

Текущая реализация индексирует только родительскую директорию решения, но нужно индексировать несколько директорий:
- `D:\galprj\XAFARI-20197\analysis\eam`
- `D:\galprj\XAFARI-20197\analysis\xafari_x024`

## Решение

Обновить `ProjectIndexService` для поддержки индексирования нескольких директорий.

## Изменения

### 1. Обновить IProjectIndexService

Добавить перегрузку метода `BuildIndex` для списка директорий:

```csharp
/// <summary>
/// Строит индекс всех проектов в указанных директориях
/// </summary>
/// <param name="rootDirectories">Список корневых директорий для индексирования</param>
void BuildIndex(IEnumerable<string> rootDirectories);
```

### 2. Обновить ProjectIndexService

Добавить реализацию метода для списка директорий:

```csharp
public void BuildIndex(IEnumerable<string> rootDirectories)
{
    if (rootDirectories == null || !rootDirectories.Any())
        throw new ArgumentException("Список директорий не может быть пустым", nameof(rootDirectories));
    
    lock (_lock)
    {
        ClearIndex();
        
        var stopwatch = Stopwatch.StartNew();
        FileLogger.Log($"ProjectIndexService: Начало построения индекса для {rootDirectories.Count()} директорий");
        
        try
        {
            foreach (var rootDirectory in rootDirectories)
            {
                if (!Directory.Exists(rootDirectory))
                {
                    FileLogger.Log($"ProjectIndexService: Директория не существует, пропускаем: {rootDirectory}");
                    continue;
                }
                
                var projectFiles = Directory.GetFiles(
                    rootDirectory, 
                    "*.csproj", 
                    SearchOption.AllDirectories
                );
                
                FileLogger.Log($"ProjectIndexService: Найдено {projectFiles.Length} файлов .csproj в {rootDirectory}");
                
                // Добавляем проекты в индекс
                int duplicateCount = 0;
                foreach (var projectFile in projectFiles)
                {
                    var projectName = Path.GetFileNameWithoutExtension(projectFile);
                    
                    if (_projectIndex.ContainsKey(projectName))
                    {
                        duplicateCount++;
                        FileLogger.Log($"ProjectIndexService: Дубликат проекта: {projectName} -> {_projectIndex[projectName]} и {projectFile}");
                    }
                    else
                    {
                        _projectIndex[projectName] = projectFile;
                    }
                    
                    var projectDir = Path.GetDirectoryName(projectFile);
                    _indexedDirectories.Add(projectDir);
                }
                
                if (duplicateCount > 0)
                {
                    FileLogger.Log($"ProjectIndexService: Обнаружено {duplicateCount} дубликатов имен проектов в {rootDirectory}");
                }
            }
            
            stopwatch.Stop();
            _buildTime = stopwatch.Elapsed;
            _isIndexBuilt = true;
            
            FileLogger.Log($"ProjectIndexService: Индекс построен за {_buildTime.TotalMilliseconds:F2} мс");
            FileLogger.Log($"ProjectIndexService: Всего проектов в индексе: {_projectIndex.Count}");
            FileLogger.Log($"ProjectIndexService: Проиндексировано директорий: {_indexedDirectories.Count}");
        }
        catch (Exception ex)
        {
            FileLogger.Log($"ProjectIndexService: Ошибка при построении индекса: {ex.Message}");
            ClearIndex();
            throw;
        }
    }
}
```

### 3. Обновить Command.cs

Изменить инициализацию индекса для использования списка директорий:

```csharp
// Инициализируем индекс проектов
// Используем родительскую директорию решения и все её поддиректории
var rootDirectory = Path.GetDirectoryName(solutionDir);
var directoriesToIndex = new List<string>();

// Добавляем родительскую директорию
if (Directory.Exists(rootDirectory))
{
    directoriesToIndex.Add(rootDirectory);
}

// Добавляем все поддиректории родительской директории
try
{
    var subDirectories = Directory.GetDirectories(rootDirectory);
    foreach (var subDir in subDirectories)
    {
        // Проверяем, содержит ли директория .csproj файлы
        var csprojFiles = Directory.GetFiles(subDir, "*.csproj", SearchOption.TopDirectoryOnly);
        if (csprojFiles.Length > 0)
        {
            directoriesToIndex.Add(subDir);
            FileLogger.Log($"Command: Добавлена директория для индексирования: {subDir} ({csprojFiles.Length} проектов)");
        }
    }
}
catch (Exception ex)
{
    FileLogger.Log($"Command: Ошибка при поиске поддиректорий: {ex.Message}");
}

if (directoriesToIndex.Count > 0)
{
    solutionService.InitializeProjectIndex(directoriesToIndex);
}
else
{
    FileLogger.Log("Command: Не найдено директорий для индексирования");
}
```

### 4. Обновить ISolutionService

Добавить перегрузку метода:

```csharp
/// <summary>
/// Initializes the project index before starting work.
/// Инициализирует индекс проектов перед началом работы.
/// </summary>
/// <param name="rootDirectories">The root directories for indexing. Корневые директории для индексирования.</param>
void InitializeProjectIndex(IEnumerable<string> rootDirectories);
```

### 5. Обновить SolutionService

Добавить реализацию метода:

```csharp
public void InitializeProjectIndex(IEnumerable<string> rootDirectories)
{
    if (rootDirectories == null || !rootDirectories.Any())
        throw new ArgumentException("Список директорий не может быть пустым", nameof(rootDirectories));
    
    FileLogger.Log($"SolutionService: Инициализация индекса проектов для {rootDirectories.Count()} директорий");
    
    using (var perf = PerformanceLogger.Measure("InitializeProjectIndex"))
    {
        _projectIndexService?.BuildIndex(rootDirectories);
        
        if (_projectIndexService != null && _projectIndexService.IsIndexBuilt)
        {
            var stats = _projectIndexService.GetStats();
            FileLogger.Log($"SolutionService: Индекс инициализирован: {stats.TotalProjects} проектов, {stats.IndexedDirectories} директорий, время: {stats.BuildTime.TotalMilliseconds:F2} мс");
        }
    }
}
```

## Преимущества

1. **Гибкость** - можно индексировать любое количество директорий
2. **Автоматическое обнаружение** - автоматически находит все поддиректории с проектами
3. **Логирование** - подробные логи для отладки
4. **Обработка ошибок** - пропускает несуществующие директории

## Ожидаемые результаты

- Индекс будут включать проекты из всех нужных директорий
- Поиск проектов будет работать мгновенно (<1 мс)
- Общее время работы сократится в 10-20 раз