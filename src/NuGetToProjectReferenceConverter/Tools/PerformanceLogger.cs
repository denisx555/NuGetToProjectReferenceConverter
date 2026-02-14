using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NuGetToProjectReferenceConverter.Tools
{
	/// <summary>
	/// Provides performance measurement and logging capabilities.
	/// Предоставляет возможности измерения производительности и логирования.
	/// </summary>
	public static class PerformanceLogger
	{
		private static readonly object _statsLock = new object();
		private static readonly Dictionary<string, PerformanceStats> _stats = new Dictionary<string, PerformanceStats>(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Creates a new performance scope for measuring operation duration.
		/// Создает новую область производительности для измерения длительности операции.
		/// </summary>
		/// <param name="operationName">The name of the operation. Имя операции.</param>
		/// <returns>A disposable performance scope. Одноразовая область производительности.</returns>
		public static PerformanceScope Measure(string operationName)
		{
			return new PerformanceScope(operationName);
		}

		/// <summary>
		/// Gets the performance statistics for all operations.
		/// Получает статистику производительности для всех операций.
		/// </summary>
		/// <returns>A dictionary of operation names and their statistics. Словарь имен операций и их статистики.</returns>
		public static Dictionary<string, PerformanceStats> GetStats()
		{
			lock (_statsLock)
			{
				return new Dictionary<string, PerformanceStats>(_stats, StringComparer.OrdinalIgnoreCase);
			}
		}

		/// <summary>
		/// Clears all performance statistics.
		/// Очищает всю статистику производительности.
		/// </summary>
		public static void ClearStats()
		{
			lock (_statsLock)
			{
				_stats.Clear();
			}
		}

		/// <summary>
		/// Logs all performance statistics to the file logger.
		/// Логирует всю статистику производительности в файловый логгер.
		/// </summary>
		public static void LogStats()
		{
			lock (_statsLock)
			{
				FileLogger.Log("=== Performance Statistics ===");
				foreach (var kvp in _stats)
				{
					var stats = kvp.Value;
					FileLogger.Log($"Operation: {kvp.Key}");
					FileLogger.Log($"  Count: {stats.Count}");
					FileLogger.Log($"  Total: {stats.TotalMilliseconds:F2} ms");
					FileLogger.Log($"  Average: {stats.AverageMilliseconds:F2} ms");
					FileLogger.Log($"  Min: {stats.MinMilliseconds:F2} ms");
					FileLogger.Log($"  Max: {stats.MaxMilliseconds:F2} ms");
				}
				FileLogger.Log("=== End Performance Statistics ===");
			}
		}

		/// <summary>
		/// Records the duration of an operation.
		/// Записывает длительность операции.
		/// </summary>
		/// <param name="operationName">The name of the operation. Имя операции.</param>
		/// <param name="duration">The duration of the operation. Длительность операции.</param>
		internal static void Record(string operationName, TimeSpan duration)
		{
			lock (_statsLock)
			{
				if (!_stats.TryGetValue(operationName, out var stats))
				{
					stats = new PerformanceStats();
					_stats[operationName] = stats;
				}
				stats.Record(duration.TotalMilliseconds);
			}
		}

		/// <summary>
		/// Represents performance statistics for an operation.
		/// Представляет статистику производительности для операции.
		/// </summary>
		public class PerformanceStats
		{
			private long _count;
			private double _totalMilliseconds;
			private double _minMilliseconds = double.MaxValue;
			private double _maxMilliseconds = double.MinValue;

			/// <summary>
			/// Gets the number of times the operation was executed.
			/// Получает количество выполнений операции.
			/// </summary>
			public long Count => _count;

			/// <summary>
			/// Gets the total execution time in milliseconds.
			/// Получает общее время выполнения в миллисекундах.
			/// </summary>
			public double TotalMilliseconds => _totalMilliseconds;

			/// <summary>
			/// Gets the average execution time in milliseconds.
			/// Получает среднее время выполнения в миллисекундах.
			/// </summary>
			public double AverageMilliseconds => _count > 0 ? _totalMilliseconds / _count : 0;

			/// <summary>
			/// Gets the minimum execution time in milliseconds.
			/// Получает минимальное время выполнения в миллисекундах.
			/// </summary>
			public double MinMilliseconds => _count > 0 ? _minMilliseconds : 0;

			/// <summary>
			/// Gets the maximum execution time in milliseconds.
			/// Получает максимальное время выполнения в миллисекундах.
			/// </summary>
			public double MaxMilliseconds => _count > 0 ? _maxMilliseconds : 0;

			/// <summary>
			/// Records a new execution duration.
			/// Записывает новую длительность выполнения.
			/// </summary>
			/// <param name="milliseconds">The duration in milliseconds. Длительность в миллисекундах.</param>
			public void Record(double milliseconds)
			{
				_count++;
				_totalMilliseconds += milliseconds;
				_minMilliseconds = Math.Min(_minMilliseconds, milliseconds);
				_maxMilliseconds = Math.Max(_maxMilliseconds, milliseconds);
			}
		}

		/// <summary>
		/// Represents a performance measurement scope.
		/// Представляет область измерения производительности.
		/// </summary>
		public class PerformanceScope : IDisposable
		{
			private readonly string _operationName;
			private readonly Stopwatch _stopwatch;

			/// <summary>
			/// Initializes a new instance of the <see cref="PerformanceScope"/> class.
			/// Инициализирует новый экземпляр класса <see cref="PerformanceScope"/>.
			/// </summary>
			/// <param name="operationName">The name of the operation. Имя операции.</param>
			public PerformanceScope(string operationName)
			{
				_operationName = operationName;
				_stopwatch = Stopwatch.StartNew();
			}

			/// <summary>
			/// Disposes the performance scope and records the duration.
			/// Освобождает область производительности и записывает длительность.
			/// </summary>
			public void Dispose()
			{
				_stopwatch.Stop();
				var duration = _stopwatch.Elapsed;
				Record(_operationName, duration);
				FileLogger.Log($"PERF: {_operationName} took {duration.TotalMilliseconds:F2} ms");
			}
		}
	}
}