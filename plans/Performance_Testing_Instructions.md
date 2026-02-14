# Инструкции по тестированию производительности

## Обзор

Добавлено логирование производительности для выявления узких мест в работе конвертера. Все ключевые методы теперь измеряют время выполнения.

---

## Выполненные изменения

### 1. Создан класс PerformanceLogger

**Файл:** `src/NuGetToProjectReferenceConverter/Tools/PerformanceLogger.cs`

**Функциональность:**
- Измерение времени выполнения операций
- Автоматическое логирование результатов
- Сбор статистики (количество, среднее, минимум, максимум)
- Поддержка вложенных операций

### 2. Добавлены замеры времени в SolutionService

**Методы с замерами времени:**
- `AddProjectToReplacedProjectsFolder` - общее время обработки проекта
- `AddProjectToReplacedProjectsFolder.LoadMSBuild` - время загрузки MSBuild проекта
- `FindProjectPathByName` - время поиска проекта в решении
- `FindProjectFileInFileSystem` - общее время поиска в файловой системе
- `FindProjectFileInFileSystem.SearchDir` - время поиска в каждой директории
- `ResolveProjectPath` - время разрешения пути к проекту

### 3. Добавлен PerformanceLogger в проект

**Файл:** `src/NuGetToProjectReferenceConverter/NuGetToProjectReferenceConverter.csproj`

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

1. В экспериментальном экземпляре откройте решение с большим количеством проектов
2. Убедитесь, что проекты имеют зависимости друг от друга

### Шаг 4: Запустить конвертер

1. Выполните команду конвертера NuGet в Project Reference
2. Дождитесь завершения процесса

### Шаг 5: Проверить логи

1. Откройте файл `logs/app.log`
2. Найдите записи с префиксом `PERF:`
3. Проанализируйте время выполнения операций

---

## Ожидаемые результаты в логах

### Формат записей производительности

```
PERF: AddProjectToReplacedProjectsFolder(ProjectName) took 123.45 ms
PERF: AddProjectToReplacedProjectsFolder.LoadMSBuild(ProjectName) took 45.67 ms
PERF: FindProjectPathByName(ProjectName) took 12.34 ms
PERF: FindProjectFileInFileSystem(ProjectName) took 234.56 ms
PERF: FindProjectFileInFileSystem.SearchDir(DirectoryName) took 123.45 ms
PERF: ResolveProjectPath(ProjectName) took 56.78 ms
```

### Пример анализа логов

```
2026-02-03 07:42:40.824 | PERF: AddProjectToReplacedProjectsFolder(Xafari.BC.Settings) took 16500.00 ms
2026-02-03 07:42:40.838 | PERF: AddProjectToReplacedProjectsFolder.LoadMSBuild(Xafari.BC.Settings) took 16400.00 ms
2026-02-03 07:42:57.343 | PERF: FindProjectPathByName(Xafari.BC) took 0.50 ms
2026-02-03 07:42:57.387 | PERF: FindProjectFileInFileSystem(Xafari.Editors) took 5000.00 ms
2026-02-03 07:42:57.387 | PERF: FindProjectFileInFileSystem.SearchDir(Xafari.BC.Settings) took 1000.00 ms
2026-02-03 07:42:57.387 | PERF: FindProjectFileInFileSystem.SearchDir(Galaktika.EAM) took 2000.00 ms
2026-02-03 07:42:57.387 | PERF: FindProjectFileInFileSystem.SearchDir(analysis) took 2000.00 ms
2026-02-03 07:42:57.387 | PERF: ResolveProjectPath(Xafari.Editors) took 5000.50 ms
```

---

## Анализ производительности

### Ключевые метрики для анализа

1. **AddProjectToReplacedProjectsFolder** - общее время обработки проекта
   - Если > 1000 ms, возможно, проект имеет много зависимостей
   - Если > 5000 ms, нужно проверить MSBuild загрузку

2. **AddProjectToReplacedProjectsFolder.LoadMSBuild** - время загрузки MSBuild проекта
   - Если > 1000 ms, возможно, проект очень большой
   - Если > 5000 ms, нужно оптимизировать загрузку

3. **FindProjectFileInFileSystem** - время поиска в файловой системе
   - Если > 1000 ms, возможно, директория содержит много файлов
   - Если > 5000 ms, нужно кэшировать список файлов

4. **FindProjectFileInFileSystem.SearchDir** - время поиска в каждой директории
   - Если > 500 ms, директория содержит много файлов
   - Если > 1000 ms, нужно оптимизировать поиск

5. **FindProjectPathByName** - время поиска в решении
   - Если > 100 ms, решение содержит много проектов
   - Если > 500 ms, нужно кэшировать список проектов

6. **ResolveProjectPath** - время разрешения пути
   - Если > 1000 ms, возможно, проект не найден и выполняется поиск в файловой системе
   - Если > 5000 ms, нужно оптимизировать поиск

---

## Возможные оптимизации

### Оптимизация 1: Кэширование списка файлов

**Проблема:** `Directory.GetFiles` выполняется каждый раз для каждой директории.

**Решение:** Кэшировать список файлов .csproj для каждой директории.

**Ожидаемый эффект:** Уменьшение времени поиска в 10-100 раз.

### Оптимизация 2: Предварительный индекс

**Проблема:** Поиск проектов выполняется каждый раз.

**Решение:** Создать индекс всех проектов в начале работы.

**Ожидаемый эффект:** Мгновенный поиск проектов.

### Оптимизация 3: Оптимизация логирования

**Проблема:** Каждое логирование записывает в файл.

**Решение:** Буферизировать логи и записывать их пакетами.

**Ожидаемый эффект:** Уменьшение времени выполнения на 10-20%.

### Оптимизация 4: Параллельный поиск

**Проблема:** Поиск выполняется последовательно.

**Решение:** Искать файлы в нескольких директориях параллельно.

**Ожидаемый эффект:** Уменьшение времени поиска в 2-3 раза.

---

## Статистика производительности

### Получение статистики

Для получения сводной статистики можно добавить вызов:

```csharp
PerformanceLogger.LogStats();
```

Это выведет в лог следующую информацию:

```
=== Performance Statistics ===
Operation: AddProjectToReplacedProjectsFolder
  Count: 10
  Total: 16500.00 ms
  Average: 1650.00 ms
  Min: 100.00 ms
  Max: 5000.00 ms
Operation: FindProjectFileInFileSystem
  Count: 5
  Total: 5000.00 ms
  Average: 1000.00 ms
  Min: 100.00 ms
  Max: 2000.00 ms
=== End Performance Statistics ===
```

---

## Диагностика проблем

### Если операция занимает слишком много времени

1. Проверьте количество вызовов операции
2. Проверьте среднее время выполнения
3. Проверьте максимальное время выполнения
4. Определите, какие операции занимают больше всего времени

### Если поиск в файловой системе медленный

1. Проверьте количество файлов в директориях
2. Проверьте глубину поиска
3. Рассмотрите возможность кэширования списка файлов

### Если загрузка MSBuild медленная

1. Проверьте размер проекта
2. Проверьте количество зависимостей
3. Рассмотрите возможность оптимизации загрузки

---

## Следующие шаги

После тестирования:

1. Проанализируйте логи производительности
2. Выявите самые медленные операции
3. Выберите оптимизации для реализации
4. Реализуйте оптимизации
5. Протестируйте снова
6. Сравните результаты до и после оптимизации

---

## Пример анализа

### До оптимизации

```
PERF: AddProjectToReplacedProjectsFolder(Project1) took 5000.00 ms
PERF: FindProjectFileInFileSystem(Project2) took 3000.00 ms
PERF: FindProjectFileInFileSystem.SearchDir(Dir1) took 1000.00 ms
PERF: FindProjectFileInFileSystem.SearchDir(Dir2) took 1000.00 ms
PERF: FindProjectFileInFileSystem.SearchDir(Dir3) took 1000.00 ms
```

**Вывод:** Поиск в файловой системе занимает 60% времени.

### После оптимизации (кэширование списка файлов)

```
PERF: AddProjectToReplacedProjectsFolder(Project1) took 2000.00 ms
PERF: FindProjectFileInFileSystem(Project2) took 100.00 ms
PERF: FindProjectFileInFileSystem.SearchDir(Dir1) took 10.00 ms
PERF: FindProjectFileInFileSystem.SearchDir(Dir2) took 10.00 ms
PERF: FindProjectFileInFileSystem.SearchDir(Dir3) took 10.00 ms
```

**Вывод:** Поиск в файловой системе занимает 5% времени. Улучшение на 95%.