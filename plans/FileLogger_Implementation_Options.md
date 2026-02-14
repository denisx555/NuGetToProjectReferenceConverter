# Варианты реализации метода Log(Exception exception)

## Обзор
Метод `Log(Exception exception)` в классе `FileLogger` должен логировать информацию об исключениях в файл лога.

---

## Вариант 1: Базовая реализация

**Описание:** Логирует только сообщение исключения и стек вызовов.

```csharp
public static void Log(Exception exception)
{
    if (exception == null)
    {
        Log("Exception is null");
        return;
    }

    string message = $"Exception: {exception.Message}{Environment.NewLine}{exception.StackTrace}";
    Log(message);
}
```

**Преимущества:**
- Простая реализация
- Использует существующий метод `Log(string message)`
- Минимальный объем кода

**Недостатки:**
- Не логирует тип исключения
- Не обрабатывает внутренние исключения
- Потеря контекста при отладке

---

## Вариант 2: Расширенная реализация

**Описание:** Логирует тип исключения, сообщение, стек вызовов и внутренние исключения.

```csharp
public static void Log(Exception exception)
{
    if (exception == null)
    {
        Log("Exception is null");
        return;
    }

    string exceptionInfo = FormatException(exception, 0);
    Log(exceptionInfo);
}

private static string FormatException(Exception exception, int level)
{
    string indent = new string(' ', level * 2);
    string result = $"{indent}Exception Type: {exception.GetType().FullName}{Environment.NewLine}";
    result += $"{indent}Message: {exception.Message}{Environment.NewLine}";
    result += $"{indent}Source: {exception.Source}{Environment.NewLine}";
    result += $"{indent}StackTrace:{Environment.NewLine}{indent}{exception.StackTrace?.Replace(Environment.NewLine, Environment.NewLine + indent)}{Environment.NewLine}";

    if (exception.InnerException != null)
    {
        result += $"{indent}Inner Exception:{Environment.NewLine}";
        result += FormatException(exception.InnerException, level + 1);
    }

    return result;
}
```

**Преимущества:**
- Полная информация об исключении
- Рекурсивная обработка внутренних исключений
- Читаемое форматирование с отступами
- Включает тип исключения и источник

**Недостатки:**
- Больше кода
- Требует дополнительного вспомогательного метода

---

## Вариант 3: Компактная реализация с использованием StringBuilder

**Описание:** Использует StringBuilder для эффективного форматирования информации об исключении.

```csharp
public static void Log(Exception exception)
{
    if (exception == null)
    {
        Log("Exception is null");
        return;
    }

    var sb = new System.Text.StringBuilder();
    FormatException(exception, sb, 0);
    Log(sb.ToString());
}

private static void FormatException(Exception exception, System.Text.StringBuilder sb, int level)
{
    string indent = new string(' ', level * 2);
    sb.AppendLine($"{indent}Exception: {exception.GetType().Name}");
    sb.AppendLine($"{indent}Message: {exception.Message}");
    sb.AppendLine($"{indent}Source: {exception.Source}");
    
    if (!string.IsNullOrEmpty(exception.StackTrace))
    {
        sb.AppendLine($"{indent}StackTrace:");
        foreach (var line in exception.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
        {
            sb.AppendLine($"{indent}  {line}");
        }
    }

    if (exception.InnerException != null)
    {
        sb.AppendLine($"{indent}--- Inner Exception ---");
        FormatException(exception.InnerException, sb, level + 1);
    }
}
```

**Преимущества:**
- Эффективное использование памяти (StringBuilder)
- Хорошее форматирование
- Рекурсивная обработка внутренних исключений

**Недостатки:**
- Больше кода
- Требует дополнительного вспомогательного метода

---

## Вариант 4: Реализация с дополнительными свойствами

**Описание:** Логирует дополнительные свойства исключения (TargetSite, HelpLink, Data).

```csharp
public static void Log(Exception exception)
{
    if (exception == null)
    {
        Log("Exception is null");
        return;
    }

    var sb = new System.Text.StringBuilder();
    FormatException(exception, sb, 0);
    Log(sb.ToString());
}

private static void FormatException(Exception exception, System.Text.StringBuilder sb, int level)
{
    string indent = new string(' ', level * 2);
    sb.AppendLine($"{indent}Exception Type: {exception.GetType().FullName}");
    sb.AppendLine($"{indent}Message: {exception.Message}");
    sb.AppendLine($"{indent}Source: {exception.Source}");
    
    if (exception.TargetSite != null)
    {
        sb.AppendLine($"{indent}TargetSite: {exception.TargetSite}");
    }
    
    if (!string.IsNullOrEmpty(exception.HelpLink))
    {
        sb.AppendLine($"{indent}HelpLink: {exception.HelpLink}");
    }
    
    if (exception.Data.Count > 0)
    {
        sb.AppendLine($"{indent}Data:");
        foreach (var key in exception.Data.Keys)
        {
            sb.AppendLine($"{indent}  {key}: {exception.Data[key]}");
        }
    }
    
    if (!string.IsNullOrEmpty(exception.StackTrace))
    {
        sb.AppendLine($"{indent}StackTrace:");
        foreach (var line in exception.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
        {
            sb.AppendLine($"{indent}  {line}");
        }
    }

    if (exception.InnerException != null)
    {
        sb.AppendLine($"{indent}--- Inner Exception ---");
        FormatException(exception.InnerException, sb, level + 1);
    }
}
```

**Преимущества:**
- Максимальная информация об исключении
- Включает все важные свойства
- Полезно для детальной отладки

**Недостатки:**
- Самый объемный вариант
- Может создавать слишком много информации в логах

---

## Вариант 5: Реализация с использованием ToString()

**Описание:** Использует встроенный метод ToString() для форматирования исключения.

```csharp
public static void Log(Exception exception)
{
    if (exception == null)
    {
        Log("Exception is null");
        return;
    }

    Log(exception.ToString());
}
```

**Преимущества:**
- Самый простой вариант
- Стандартное форматирование .NET
- Включает внутренние исключения

**Недостатки:**
- Нет контроля над форматированием
- Меньшая читаемость по сравнению с кастомным форматированием

---

## Рекомендация

Для расширения NuGetToProjectReferenceConverter рекомендуется **Вариант 2** или **Вариант 3**, так как они:
- Предоставляют полную информацию об исключениях
- Обрабатывают внутренние исключения рекурсивно
- Имеют читаемое форматирование
- Подходят для отладки и диагностики проблем

**Вариант 3** предпочтительнее, если ожидается много исключений с большими стеками вызовов, благодаря использованию StringBuilder.