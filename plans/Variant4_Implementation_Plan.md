# План реализации Варианта 4: Оптимизированный поиск в файловой системе с кэшированием

## Обзор

Вариант 4 добавляет кэширование найденных проектов для оптимизации производительности при работе с большими решениями.

---

## Задачи

### 1. Добавить поле для кэша проектов

**Файл:** `src/NuGetToProjectReferenceConverter/Services/Solutions/SolutionService.cs`

**Действие:** Добавить приватное поле для кэша проектов

```csharp
private readonly Dictionary<string, string> _projectCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
```

**Расположение:** После строки 23 (после `_replacedProjectsFolder`)

---

### 2. Реализовать метод FindProjectFileInFileSystem с кэшированием

**Файл:** `src/NuGetToProjectReferenceConverter/Services/Solutions/SolutionService.cs`

**Действие:** Добавить новый приватный метод после метода `FindProjectPathByName`

**Метод:**
```csharp
/// <summary>
/// Finds a project file by name in the solution directory recursively with caching.
/// Находит файл проекта по имени в директории решения рекурсивно с кэшированием.
/// </summary>
/// <param name="projectName">The project name to search for. Имя проекта для поиска.</param>
/// <param name="solutionDir">The solution directory. Директория решения.</param>
/// <returns>The absolute path to the project file, or null if not found. Абсолютный путь к файлу проекта или null, если не найден.</returns>
private string FindProjectFileInFileSystem(string projectName, string solutionDir)
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

    // Если кэш пуст или файл не существует, выполняем поиск
    try
    {
        var csprojFiles = Directory.GetFiles(solutionDir, "*.csproj", SearchOption.AllDirectories);
        foreach (var file in csprojFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            if (fileName.Equals(projectName, StringComparison.OrdinalIgnoreCase))
            {
                // Добавляем в кэш
                _projectCache[projectName] = file;
                FileLogger.Log($"SolutionService.FindProjectFileInFileSystem: Файл проекта найден в файловой системе: {projectName} -> {file}");
                return file;
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
```

**Расположение:** После метода `FindProjectPathByName` (примерно строка 110)

---

### 3. Обновить метод ResolveProjectPath

**Файл:** `src/NuGetToProjectReferenceConverter/Services/Solutions/SolutionService.cs`

**Действие:** Добавить поиск в файловой системе в метод `ResolveProjectPath`

**Изменения:**
```csharp
private string ResolveProjectPath(string basePath, string relativePath, string solutionDir)
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

    // 4. Пробуем поиск по имени в решении
    var projectName = Path.GetFileNameWithoutExtension(relativePath);
    var foundPath = FindProjectPathByName(projectName);
    if (foundPath != null && File.Exists(foundPath))
    {
        FileLogger.Log($"SolutionService.ResolveProjectPath: Путь найден по имени в решении: {foundPath}");
        return foundPath;
    }

    // 5. Пробуем поиск по имени в файловой системе с кэшированием
    var foundPathInFs = FindProjectFileInFileSystem(projectName, solutionDir);
    if (foundPathInFs != null && File.Exists(foundPathInFs))
    {
        FileLogger.Log($"SolutionService.ResolveProjectPath: Путь найден по имени в файловой системе: {foundPathInFs}");
        return foundPathInFs;
    }

    // 6. Возвращаем первый вариант (вызовет исключение, если файл не существует)
    FileLogger.Log($"SolutionService.ResolveProjectPath: Путь не найден ни одним способом, возвращаем первый вариант: {path1}");
    return path1;
}
```

**Расположение:** Обновить существующий метод `ResolveProjectPath` (примерно строка 110)

---

## Порядок выполнения

1. ✅ Добавить поле `_projectCache` в класс `SolutionService`
2. ✅ Реализовать метод `FindProjectFileInFileSystem` с кэшированием
3. ✅ Обновить метод `ResolveProjectPath` для использования поиска в файловой системе

---

## Преимущества реализации

1. **Кэширование:** Повторные запросы к одному и тому же проекту обрабатываются мгновенно
2. **Надежность:** Находит проекты, которые не загружены в решение
3. **Производительность:** Оптимизировано для больших решений
4. **Детальное логирование:** Полная информация о процессе разрешения путей

---

## Тестирование

После реализации необходимо протестировать:

1. **Сценарий 1:** Проект загружен в решение (Xafari.BC)
2. **Сценарий 2:** Проект не загружен в решение, но существует в файловой системе (Xafari.Editors)
3. **Сценарий 3:** Повторный запрос к тому же проекту (должен использовать кэш)

---

## Ожидаемый результат

После реализации:

- Проект `Xafari.BC` будет найден через поиск в решении
- Проект `Xafari.Editors` будет найден через поиск в файловой системе
- Повторные запросы к проектам будут использовать кэш
- Логи будут содержать детальную информацию о процессе разрешения путей