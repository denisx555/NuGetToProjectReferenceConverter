using System;
using System.Diagnostics;
using System.IO;

namespace NuGetToProjectReferenceConverter.Tools
{
	public static class FileLogger
	{
		private static string GetProjectRootPath()
		{
			//string currentDir = AppContext.BaseDirectory;
			//DirectoryInfo dirInfo = new DirectoryInfo(currentDir);
			//for (int i = 0; i < 6; i++)
			//{
			//	if (dirInfo.Parent != null)
			//	{
			//		dirInfo = dirInfo.Parent;
			//	}
			//	else
			//	{
			//		return dirInfo.FullName;
			//	}
			//}
			//return dirInfo.FullName;

			return "D:\\galprj\\Tools\\NuGetToProjectReferenceConverter";
		}

		private static readonly string logFilePath = Path.Combine(GetProjectRootPath(), "logs", "app.log");
		private static readonly object lockObj = new object();
		private static bool _logClear = false;

		public static void Log(string message, bool stacktrace = false)
		{
			try
			{
				if (stacktrace)
				{
					message = $"{message}{Environment.NewLine}{Environment.StackTrace}";
				}

				lock (lockObj)
				{
					string logDirectory = Path.GetDirectoryName(logFilePath);
					if (!Directory.Exists(logDirectory))
					{
						Directory.CreateDirectory(logDirectory);
					}

					if (!_logClear && File.Exists(logFilePath))
					{
						_logClear = true;
						File.Delete(logFilePath);
					}

					string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | {message}{Environment.NewLine}";
					File.AppendAllText(logFilePath, logMessage);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Error in  FileLogger: {ex.Message}");				
			}
		}

		/// <summary>
		/// Logs an exception with full details including type, message, stack trace, target site, help link, data and inner exceptions.
		/// Логирует исключение с полными деталями, включая тип, сообщение, стек вызовов, целевой метод, ссылку на справку, данные и внутренние исключения.
		/// </summary>
		/// <param name="exception">The exception to log. Исключение для логирования.</param>
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

		/// <summary>
		/// Formats an exception recursively with indentation for inner exceptions.
		/// Форматирует исключение рекурсивно с отступами для внутренних исключений.
		/// </summary>
		/// <param name="exception">The exception to format. Исключение для форматирования.</param>
		/// <param name="sb">The string builder to write to. Строитель строк для записи.</param>
		/// <param name="level">The indentation level. Уровень отступа.</param>
		private static void FormatException(Exception exception, System.Text.StringBuilder sb, int level)
		{
			string indent = new string(' ', level * 2);
			
			// Основная информация об исключении
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
			
			// StackTrace - стек вызовов
			if (!string.IsNullOrEmpty(exception.StackTrace))
			{
				sb.AppendLine($"{indent}StackTrace:");
				foreach (var line in exception.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
				{
					sb.AppendLine($"{indent}  {line}");
				}
			}

			// InnerException - внутреннее исключение (рекурсивная обработка)
			if (exception.InnerException != null)
			{
				sb.AppendLine($"{indent}--- Inner Exception ---");
				FormatException(exception.InnerException, sb, level + 1);
			}
		}
	}
}