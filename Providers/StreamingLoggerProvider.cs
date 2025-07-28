/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

using System.Collections.Concurrent;

namespace Arcadeia.Providers
{
   public class StreamingLoggerProvider : ILoggerProvider
   {
      private readonly ConcurrentQueue<string> _logQueue = new();

      public event Action<string>? OnLog;

      public ILogger CreateLogger(string categoryName)
      {
         return new StreamingLogger(categoryName, (msg) =>
         {
            _logQueue.Enqueue(msg);
            OnLog?.Invoke(msg);
         });
      }

      public void Dispose()
      {
         GC.SuppressFinalize(this);
      }

      private class StreamingLogger(string categoryName, Action<string> onLog) : ILogger
      {
         private readonly string _categoryName = categoryName;
         private readonly Action<string> _onLog = onLog;

         IDisposable ILogger.BeginScope<TState>(TState state) => null!;

         public bool IsEnabled(LogLevel logLevel) => true;

         public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
         {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string message = $"[{timestamp}] {_categoryName} [{logLevel}] {formatter(state, exception)}";
            _onLog(message);
         }
      }
   }
}
