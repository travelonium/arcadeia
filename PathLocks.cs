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

namespace Arcadeia
{
   /// <summary>
   /// Keeps track of the paths that are locked while being moved. A path ending in a separator is
   /// a folder and locks everything under it as well.
   /// </summary>
   public class PathLocks
   {
      private readonly object _lock = new();
      private readonly List<string> _locked = [];

      public IDisposable Lock(params string[] paths)
      {
         lock (_lock)
         {
            _locked.AddRange(paths);
         }

         return new Releaser(this, paths);
      }

      public bool IsLocked(string? path)
      {
         if (path == null) return false;

         lock (_lock)
         {
            return _locked.Any(locked => IsWithin(path, locked));
         }
      }

      /// <summary>
      /// Checks whether the path is the other path or, if the other is a folder, anywhere under it.
      /// </summary>
      public static bool IsWithin(string path, string other)
      {
         return path == other || ((other.EndsWith('/') || other.EndsWith('\\')) && path.StartsWith(other, StringComparison.Ordinal));
      }

      private void Release(string[] paths)
      {
         lock (_lock)
         {
            foreach (var path in paths)
            {
               _locked.Remove(path);
            }
         }
      }

      private sealed class Releaser(PathLocks locks, string[] paths) : IDisposable
      {
         private bool _disposed = false;

         public void Dispose()
         {
            if (_disposed) return;

            _disposed = true;
            locks.Release(paths);
         }
      }
   }
}
