// using System.Collections.Concurrent;
// using System.Reflection;
//
// namespace Darlin.Loggers;
//
// public class CsvLogger<T> : IDisposable where T : class
// {
//     private readonly CancellationTokenSource _cancellationTokenSource = new();
//     private readonly string _directoryPath;
//     private readonly string _fileName;
//     private readonly Task _processingTask;
//     private readonly PropertyInfo[] _properties;
//     private readonly ConcurrentQueue<T> _queue = new();
//
//     public CsvLogger(string directoryPath, string fileName)
//     {
//         _directoryPath = directoryPath;
//         _fileName = fileName;
//         Directory.CreateDirectory(_directoryPath);
//         _properties = typeof(T).GetProperties();
//         _processingTask = Task.Run(ProcessQueueAsync, _cancellationTokenSource.Token);
//     }
//
//     public void Dispose()
//     {
//         _cancellationTokenSource.Cancel();
//         _processingTask.Wait();
//         _cancellationTokenSource.Dispose();
//         GC.SuppressFinalize(this);
//     }
//
//     /// <summary>
//     ///     Enqueues an item to be written to CSV.
//     /// </summary>
//     public void Log(T dto)
//     {
//         _queue.Enqueue(dto);
//     }
//
//     /// <summary>
//     ///     Processes the queue in the background and writes each DTO as a CSV line.
//     /// </summary>
//     private async Task ProcessQueueAsync()
//     {
//         var fullPath = Path.Combine(_directoryPath, _fileName);
//         var headerWritten = File.Exists(fullPath);
//         while (!_cancellationTokenSource.Token.IsCancellationRequested)
//             if (_queue.TryPeek(out var dto))
//                 try
//                 {
//                     // Write to file in append mode.
//                     using (var writer = new StreamWriter(fullPath, true))
//                     {
//                         if (!headerWritten)
//                         {
//                             // Write CSV header if the file did not exist.
//                             await writer.WriteLineAsync(GetCsvHeader());
//                             headerWritten = true;
//                         }
//
//                         var line = FormatCsvLine(dto);
//                         await writer.WriteLineAsync(line);
//                     }
//
//                     // If writing is successful, remove the DTO from the queue.
//                     _queue.TryDequeue(out _);
//                 }
//                 catch (Exception ex)
//                 {
//                     // Log the error and wait briefly before retrying.
//                     Console.WriteLine($"Error writing to CSV: {ex.Message}");
//                     await Task.Delay(1000);
//                 }
//             else
//                 // No items in the queue, wait for a short time.
//                 await Task.Delay(500);
//     }
//
//     /// <summary>
//     ///     Generates the CSV header by using the property names of T.
//     /// </summary>
//     private string GetCsvHeader()
//     {
//         return string.Join(",", _properties.Select(p => p.Name));
//     }
//
//     /// <summary>
//     ///     Formats an instance of T as a single CSV line using its public properties.
//     /// </summary>
//     private string FormatCsvLine(T dto)
//     {
//         return string.Join(",", _properties.Select(p => FormatValue(p.GetValue(dto))));
//     }
//
//     /// <summary>
//     ///     Converts a value to its string representation with proper formatting.
//     /// </summary>
//     private string FormatValue(object value)
//     {
//         if (value == null)
//             return string.Empty;
//
//         string result;
//         if (value is DateTime dt)
//             // Using the ISO 8601 format for DateTime.
//             result = dt.ToString("o");
//         else if (value is TimeSpan ts)
//             result = ts.ToString("c"); // Constant ("c") format for TimeSpan.
//         else
//             result = value.ToString();
//
//         // Escape commas or quotes if needed.
//         if (result.Contains(",") || result.Contains("\"")) result = $"\"{result.Replace("\"", "\"\"")}\"";
//
//         return result;
//     }
// }