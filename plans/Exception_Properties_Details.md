# Подробное описание свойств Exception

## Обзор
Класс `Exception` в .NET содержит несколько полезных свойств, которые помогают при отладке и диагностике проблем.

---

## 1. TargetSite

**Тип:** `System.Reflection.MethodBase`

**Описание:** Возвращает метод, который выбросил исключение.

**Что содержит:**
- Имя метода (например, `AddProjectToReplacedProjectsFolder`)
- Имя класса (например, `SolutionService`)
- Информацию о параметрах (если доступна)
- Модификаторы доступа (public, private и т.д.)

**Пример вывода:**
```
TargetSite: Void AddProjectToReplacedProjectsFolder(System.String, System.Collections.Generic.List`1[System.String])
```

**Полезность:**
- Быстро определяет точное место возникновения исключения
- Помогает понять, какой метод вызвал проблему
- Полезно при анализе логов для быстрого поиска проблемного кода

**Пример использования:**
```csharp
try
{
    // код, который может выбросить исключение
}
catch (Exception ex)
{
    Console.WriteLine($"Метод с ошибкой: {ex.TargetSite.Name}");
    Console.WriteLine($"Класс: {ex.TargetSite.DeclaringType?.Name}");
}
```

---

## 2. HelpLink

**Тип:** `string`

**Описание:** Ссылка на файл справки или URL с дополнительной информацией об исключении.

**Что содержит:**
- URL на документацию (для стандартных исключений .NET)
- Путь к локальному файлу справки
- Может быть пустым (null или пустая строка)

**Пример вывода:**
```
HelpLink: https://docs.microsoft.com/en-us/dotnet/api/system.io.filenotfoundexception
```

**Полезность:**
- Предоставляет контекстную документацию
- Помогает понять причины исключения
- Полезно для стандартных исключений .NET, которые имеют документацию

**Пример использования:**
```csharp
try
{
    // код, который может выбросить исключение
}
catch (Exception ex)
{
    if (!string.IsNullOrEmpty(ex.HelpLink))
    {
        Console.WriteLine($"Документация: {ex.HelpLink}");
    }
}
```

**Важно:** В пользовательских исключениях это свойство обычно пустое, если разработчик явно не установил его.

---

## 3. Data

**Тип:** `System.Collections.IDictionary`

**Описание:** Коллекция пар "ключ-значение" с дополнительной информацией об исключении.

**Что содержит:**
- Произвольные данные, добавленные при создании исключения
- Контекстную информацию, специфичную для приложения
- Может быть пустым (Count == 0)

**Пример вывода:**
```
Data:
  ProjectPath: D:\MyProject\MyProject.csproj
  AttemptedOperation: AddProjectToReplacedProjectsFolder
  Timestamp: 2026-02-03T03:34:46.239Z
```

**Полезность:**
- Позволяет добавить контекстную информацию к исключению
- Помогает воспроизвести проблему
- Полезно для логирования специфичных данных приложения

**Пример использования:**
```csharp
try
{
    // код, который может выбросить исключение
}
catch (Exception ex)
{
    // Добавление данных к исключению
    ex.Data["ProjectPath"] = projectPath;
    ex.Data["Operation"] = "AddProject";
    ex.Data["Timestamp"] = DateTime.Now;
    
    // Логирование
    Log(ex);
}
```

**Пример создания пользовательского исключения с данными:**
```csharp
public class ProjectConversionException : Exception
{
    public ProjectConversionException(string message, string projectPath) 
        : base(message)
    {
        Data["ProjectPath"] = projectPath;
        Data["ExceptionTime"] = DateTime.Now;
    }
}
```

---

## Сравнительная таблица

| Свойство | Тип | Всегда ли заполнено | Полезность для отладки |
|----------|-----|---------------------|------------------------|
| **Message** | string | Да | Высокая - описание ошибки |
| **StackTrace** | string | Обычно | Очень высокая - место возникновения |
| **TargetSite** | MethodBase | Обычно | Высокая - точный метод |
| **HelpLink** | string | Редко | Средняя - документация |
| **Data** | IDictionary | Редко | Высокая - контекст приложения |
| **Source** | string | Обычно | Средняя - сборка |
| **InnerException** | Exception | Иногда | Очень высокая - причина ошибки |

---

## Пример полного логирования с использованием всех свойств

```csharp
public static void Log(Exception exception)
{
    if (exception == null)
    {
        Log("Exception is null");
        return;
    }

    var sb = new StringBuilder();
    FormatException(exception, sb, 0);
    Log(sb.ToString());
}

private static void FormatException(Exception exception, StringBuilder sb, int level)
{
    string indent = new string(' ', level * 2);
    
    // Основная информация
    sb.AppendLine($"{indent}Exception Type: {exception.GetType().FullName}");
    sb.AppendLine($"{indent}Message: {exception.Message}");
    sb.AppendLine($"{indent}Source: {exception.Source}");
    
    // TargetSite - метод, выбросивший исключение
    if (exception.TargetSite != null)
    {
        sb.AppendLine($"{indent}TargetSite: {exception.TargetSite}");
    }
    
    // HelpLink - ссылка на документацию
    if (!string.IsNullOrEmpty(exception.HelpLink))
    {
        sb.AppendLine($"{indent}HelpLink: {exception.HelpLink}");
    }
    
    // Data - дополнительные данные
    if (exception.Data.Count > 0)
    {
        sb.AppendLine($"{indent}Data:");
        foreach (var key in exception.Data.Keys)
        {
            sb.AppendLine($"{indent}  {key}: {exception.Data[key]}");
        }
    }
    
    // StackTrace
    if (!string.IsNullOrEmpty(exception.StackTrace))
    {
        sb.AppendLine($"{indent}StackTrace:");
        foreach (var line in exception.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
        {
            sb.AppendLine($"{indent}  {line}");
        }
    }

    // InnerException - внутреннее исключение
    if (exception.InnerException != null)
    {
        sb.AppendLine($"{indent}--- Inner Exception ---");
        FormatException(exception.InnerException, sb, level + 1);
    }
}
```

---

## Рекомендации для NuGetToProjectReferenceConverter

### Для расширения NuGetToProjectReferenceConverter:

1. **TargetSite** - **Рекомендуется включать**
   - Поможет быстро определить, в каком методе произошла ошибка
   - Особенно полезно при работе с решениями и проектами

2. **HelpLink** - **Можно включить**
   - Для стандартных исключений .NET может быть полезна
   - Не займет много места в логах

3. **Data** - **Настоятельно рекомендуется включать**
   - Позволит добавлять контекстную информацию (пути к проектам, имена папок и т.д.)
   - Критически важно для воспроизведения проблем

### Пример добавления контекстных данных в SolutionService:

```csharp
catch (Exception ex)
{
    // Добавляем контекстную информацию
    ex.Data["ProjectPath"] = projectPath;
    ex.Data["MethodName"] = "AddProjectToReplacedProjectsFolder";
    ex.Data["AddedListCount"] = addedList?.Count ?? 0;
    
    FileLogger.Log(ex);
    throw;
}
```

---

## Итог

Для расширения NuGetToProjectReferenceConverter рекомендуется использовать **Вариант 4** (с дополнительными свойствами), так как:

1. **TargetSite** поможет быстро найти проблемный метод
2. **Data** позволит сохранять важный контекст (пути к проектам, состояние операции)
3. **HelpLink** может быть полезен для стандартных исключений .NET

Это обеспечит максимально детальную информацию для отладки и диагностики проблем.