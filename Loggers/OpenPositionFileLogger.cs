// using System.Collections.Concurrent;
// using Darlin.Domain.Models;
// using Newtonsoft.Json;
//
// namespace Darlin.Loggers;
//
// public class OpenPositionFileLogger : IDisposable
// {
//     private readonly ReaderWriterLockSlim _fileLock = new();
//     private readonly string _filePath;
//     private readonly ConcurrentDictionary<string, PositionInfo> _positions = new();
//
//     /// <summary>
//     ///     Initializes a new instance, loading existing positions if the file exists.
//     /// </summary>
//     /// <param name="filePath">Path to the JSON file for storing open positions.</param>
//     public OpenPositionFileLogger(string filePath)
//     {
//         if (string.IsNullOrWhiteSpace(filePath))
//             throw new ArgumentException("File path must be provided", nameof(filePath));
//
//         _filePath = filePath;
//         LoadFromFile();
//     }
//
//     /// <summary>
//     ///     Disposes internal locks.
//     /// </summary>
//     public void Dispose()
//     {
//         _fileLock?.Dispose();
//     }
//
//     private void LoadFromFile()
//     {
//         try
//         {
//             if (File.Exists(_filePath))
//             {
//                 _fileLock.EnterReadLock();
//                 var json = File.ReadAllText(_filePath);
//                 var dict = JsonConvert.DeserializeObject<ConcurrentDictionary<string, PositionInfo>>(json);
//                 if (dict != null)
//                     foreach (var kv in dict)
//                         _positions[kv.Key] = kv.Value;
//             }
//             else
//             {
//                 EnsureDirectoryExists();
//                 WriteToFile(); // create an empty file for the first time
//             }
//         }
//         finally
//         {
//             if (_fileLock.IsReadLockHeld)
//                 _fileLock.ExitReadLock();
//         }
//     }
//
//     private void EnsureDirectoryExists()
//     {
//         var directory = Path.GetDirectoryName(_filePath);
//         if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
//             Directory.CreateDirectory(directory);
//     }
//
//     /// <summary>
//     ///     Adds or updates an open position for the given ticker.
//     /// </summary>
//     /// <param name="ticker">The symbol identifier (e.g., "BTCUSDT").</param>
//     /// <param name="position">The position info to store.</param>
//     public void Add(string ticker, PositionInfo position)
//     {
//         if (string.IsNullOrWhiteSpace(ticker))
//             throw new ArgumentException("Ticker must be provided", nameof(ticker));
//         if (position == null)
//             throw new ArgumentNullException(nameof(position));
//
//         _positions[ticker] = position;
//         WriteToFile();
//     }
//
//     /// <summary>
//     ///     Removes the open position entry for the given ticker.
//     /// </summary>
//     /// <param name="ticker">The symbol identifier to remove.</param>
//     public void Remove(string ticker)
//     {
//         if (string.IsNullOrWhiteSpace(ticker))
//             throw new ArgumentException("Ticker must be provided", nameof(ticker));
//
//         _positions.TryRemove(ticker, out _);
//         WriteToFile();
//     }
//
//     private void WriteToFile()
//     {
//         EnsureDirectoryExists();
//         _fileLock.EnterWriteLock();
//         try
//         {
//             var json = JsonConvert.SerializeObject(_positions, Formatting.Indented);
//             File.WriteAllText(_filePath, json);
//         }
//         finally
//         {
//             _fileLock.ExitWriteLock();
//         }
//     }
// }