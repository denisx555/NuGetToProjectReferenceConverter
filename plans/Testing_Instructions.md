# Инструкции по тестированию решения

## Обзор

Реализован **Вариант 4 (Комбинированный подход)** для решения проблемы с поиском проектов.

## Выполненные изменения

### 1. Обновлен метод `FindProjectFileInFileSystem`

**Файл:** `src/NuGetToProjectReferenceConverter/Services/Solutions/SolutionService.cs`

**Изменения:**
- Добавлен параметр `currentProjectDir` для передачи директории текущего проекта
- Добавлен параметр `maxParentLevels` для настройки глубины поиска в родительских директориях
- Реализован комбинированный поиск в следующих директориях (в порядке приоритета):
  1. Директория текущего проекта (если указана)
  2. Директория решения
  3. Родительские директории решения (до 2 уровней по умолчанию)

### 2. Обновлен метод `ResolveProjectPath`

**Файл:** `src/NuGetToProjectReferenceConverter/Services/Solutions/SolutionService.cs`

**Изменения:**
- Обновлен вызов `FindProjectFileInFileSystem` с передачей директории текущего проекта (`basePath`)
- Теперь используется комбинированный подход для поиска проектов

---

## Как протестировать

### Шаг 1: Скомпилировать проект

1. Откройте Visual Studio
2. Откройте решение `NuGetToProjectReferenceConverter.sln`
3. Выполните сборку проекта (Build → Build Solution или Ctrl+Shift+B)

### Шаг 2: Запустить расширение

1. Запустите отладку (Debug → Start Debugging или F5)
2. Откроется экспериментальный экземпляр Visual Studio

### Шаг 3: Открыть тестовое решение

1. В экспериментальном экземпляре откройте решение, которое содержит проект `Xafari.BC.Settings`
2. Убедитесь, что проект `Xafari.Editors` существует в файловой системе, но не загружен в решение

### Шаг 4: Запустить конвертер

1. Выполните команду конвертера NuGet в Project Reference
2. Дождитесь завершения процесса

### Шаг 5: Проверить логи

1. Откройте файл `logs/app.log`
2. Найдите записи, связанные с проектом `Xafari.Editors`
3. Проверьте, что проект был успешно найден и добавлен

---

## Ожидаемые результаты

### Успешный сценарий

В логах должны быть следующие записи:

```
SolutionService.FindProjectFileInFileSystem: Поиск файла проекта: Xafari.Editors в директории: D:\galprj\XAFARI-20197\analysis\xafari_x024\Xafari\Xafari.BC.Settings
SolutionService.FindProjectFileInFileSystem: Найдено X файлов .csproj
SolutionService.FindProjectFileInFileSystem: Поиск файла проекта: Xafari.Editors в директории: D:\galprj\XAFARI-20197\analysis\eam\Galaktika.EAM
SolutionService.FindProjectFileInFileSystem: Найдено Y файлов .csproj
SolutionService.FindProjectFileInFileSystem: Поиск файла проекта: Xafari.Editors в директории: D:\galprj\XAFARI-20197\analysis
SolutionService.FindProjectFileInFileSystem: Найдено Z файлов .csproj
SolutionService.FindProjectFileInFileSystem: Файл проекта найден: Xafari.Editors -> D:\galprj\XAFARI-20197\analysis\xafari_x024\Xafari\Xafari.Editors\Xafari.Editors.csproj
SolutionService.ResolveProjectPath: Путь найден по имени в файловой системе: D:\galprj\XAFARI-20197\analysis\xafari_x024\Xafari\Xafari.Editors\Xafari.Editors.csproj
SolutionService.AddProjectToReplacedProjectsFolder: Добавление проекта: D:\galprj\XAFARI-20197\analysis\xafari_x024\Xafari\Xafari.Editors\Xafari.Editors.csproj
SolutionService.AddProjectToReplacedProjectsFolder: Проект добавлен в папку и в список
```

**Ключевые моменты:**
- Поиск выполняется в нескольких директориях
- Проект `Xafari.Editors` найден в родительской директории решения
- Нет исключения `FileNotFoundException`

---

## Диагностика проблем

### Если проект все еще не найден

1. Проверьте, что файл `Xafari.Editors.csproj` действительно существует в файловой системе
2. Проверьте логи для определения, в каких директориях выполнялся поиск
3. Убедитесь, что путь к решению корректный

### Если возникла ошибка

1. Проверьте полный стек-трейс в логах
2. Убедитесь, что все необходимые права доступа к файлам есть
3. Проверьте, что файлы проектов не заблокированы другим процессом

---

## Дополнительная настройка

### Изменение глубины поиска в родительских директориях

Если нужно искать проекты глубже в иерархии, можно изменить значение параметра `maxParentLevels`:

```csharp
// В методе FindProjectFileInFileSystem
private string FindProjectFileInFileSystem(string projectName, string solutionDir, string currentProjectDir = null, int maxParentLevels = 2)
```

Значение по умолчанию: `2` (ищет в директории решения и в 2 родительских директориях)

---

## Следующие шаги

После успешного тестирования:

1. Проверьте логи на наличие ошибок
2. Убедитесь, что все проекты успешно добавлены в папку `!ReplacedProjects`
3. Проверьте, что ссылки на проекты корректно обновлены
4. При необходимости настройте глубину поиска (`maxParentLevels`)

---

## Контакты

Если возникнут вопросы или проблемы, обратитесь к файлу `plans/Problem_Solution_Variants.md` для получения дополнительной информации о вариантах решения.