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

using Arcadeia.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Arcadeia
{
   class ThumbnailsDatabase : IThumbnailsDatabase
   {
      private readonly IOptionsMonitor<Settings> _settings;

      private readonly ILogger<ThumbnailsDatabase> _logger;

      public readonly Dictionary<string, string> Columns = [];

      private string ConnectionString => new(new SqliteConnectionStringBuilder
      {
         Pooling = true,
         DataSource = FullPath,
         Cache = SqliteCacheMode.Shared,
      }.ToString());

      /// <summary>
      /// The directory in which where the database file is to be found or created.
      /// </summary>
      public string Path => _settings.CurrentValue.Thumbnails.Database.Path;

      /// <summary>
      /// The full path to the database file.
      /// </summary>
      public string FullPath => System.IO.Path.Combine(_settings.CurrentValue.Thumbnails.Database.Path, _settings.CurrentValue.Thumbnails.Database.Name);

      /// <summary>
      /// The maximum count of thumbnails the database is able to store.
      /// </summary>
      public int Count => new[]
      {
         _settings.CurrentValue.Thumbnails.Video,
         _settings.CurrentValue.Thumbnails.Photo,
         _settings.CurrentValue.Thumbnails.Audio
      }
      .SelectMany(x => x.Values).Where(x => !x.Sprite && x.Count > 0).Max(x => x.Count);

      /// <summary>
      /// Initializes a new instance of the <see cref="ThumbnailsDatabase"/> class.
      /// </summary>
      public ThumbnailsDatabase(IOptionsMonitor<Settings> settings, ILogger<ThumbnailsDatabase> logger)
      {
         _logger = logger;
         _settings = settings;

         // Check if the Thumbnails Database file already exists.
         if (!File.Exists(FullPath))
         {
            try
            {
               // Create the hosting directory
               Directory.CreateDirectory(Path);

               // Create the new database file
               using SqliteConnection connection = new(ConnectionString);
               connection.Open();

               _logger.LogInformation("Thumbnails Database Created: {}", FullPath);
            }
            catch (Exception e)
            {
               _logger.LogError("Thumbnails Database Creation Failed! Because: {}", e.Message);
            }
         }

         // Update the database layout if needed.
         UpdateDatabaseLayout();

         // Retrieve and store the list of all the columns
         Columns = GetColumns("Thumbnails");
      }

      /// <summary>
      /// Finalizes an instance of the <see cref="ThumbnailsDatabase"/> class.
      /// </summary>
      ~ThumbnailsDatabase()
      {
      }

      #region Interface

      public void Create(string id)
      {
         if (!RowExists("Thumbnails", "ID", id))
         {
            AddRow("Thumbnails", "ID", id);
         }
      }

      public bool Exists(string id)
      {
         return RowExists("Thumbnails", "ID", id);
      }

      public void SetThumbnail(string id, int index, ref byte[] data)
      {
         if (!RowExists("Thumbnails", "ID", id))
         {
            AddRow("Thumbnails", "ID", id);
         }

         string column = "T" + index.ToString();
         string sql = "UPDATE Thumbnails SET " + column + "= @" + column + " WHERE ID='" + id + "'";

         using SqliteConnection connection = new(ConnectionString);
         connection.Open();

         using SqliteCommand command = new(sql, connection);
         command.Parameters.Add("@" + column, SqliteType.Blob).Value = data;
         command.ExecuteNonQuery();
      }

      public void SetThumbnail(string id, string label, ref byte[] data)
      {
         if (!RowExists("Thumbnails", "ID", id))
         {
            AddRow("Thumbnails", "ID", id);
         }

         string column = label.ToUpper();
         string sql = "UPDATE Thumbnails SET " + column + "= @" + column + " WHERE ID='" + id + "'";

         using SqliteConnection connection = new(ConnectionString);
         connection.Open();

         using SqliteCommand command = new(sql, connection);
         command.Parameters.Add("@" + column, SqliteType.Blob).Value = data;
         command.ExecuteNonQuery();
      }

      public byte[] GetThumbnail(string id, string label)
      {
         string column = label.ToUpper();
         byte[] thumbnail = Array.Empty<byte>();
         string sql = "SELECT " + column + " FROM Thumbnails WHERE ID='" + id + "'";

         if (!Columns.ContainsKey(column)) return thumbnail;

         using (SqliteConnection connection = new(ConnectionString))
         {
            connection.Open();

            using SqliteCommand command = new(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               int ordinal = reader.GetOrdinal(column);

               if (!reader.IsDBNull(ordinal))
               {
                  object blob = reader[column];

                  if (blob.GetType() == typeof(byte[]))
                  {
                     thumbnail = (byte[])blob;

                     break;
                  }
               }
            }
         }

         return thumbnail;
      }

      public byte[] GetThumbnail(string id, int index)
      {
         return GetThumbnail(id, "T" + index.ToString());
      }

      public async Task<byte[]> GetThumbnailAsync(string id, string label, CancellationToken cancellationToken)
      {
         string column = label.ToUpper();
         byte[] thumbnail = Array.Empty<byte>();
         string sql = "SELECT " + column + " FROM Thumbnails WHERE ID='" + id + "'";

         if (!Columns.ContainsKey(column)) return thumbnail;

         using (SqliteConnection connection = new(ConnectionString))
         {
            await connection.OpenAsync(cancellationToken);

            using SqliteCommand command = new(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
               int ordinal = reader.GetOrdinal(column);

               if (!await reader.IsDBNullAsync(ordinal, cancellationToken))
               {
                  object blob = reader[column];

                  if (blob.GetType() == typeof(byte[]))
                  {
                     thumbnail = (byte[])blob;

                     break;
                  }
               }
            }
         }

         return thumbnail;
      }

      public async Task<byte[]> GetThumbnailAsync(string id, int index, CancellationToken cancellationToken)
      {
         return await GetThumbnailAsync(id, "T" + index.ToString(), cancellationToken);
      }

      public int DeleteThumbnails(string id)
      {
         return DeleteRow("Thumbnails", "ID", id);
      }

      public int GetThumbnailsCount(string id)
      {
         int count = 0;
         string sql = "SELECT * FROM Thumbnails WHERE ID='" + id + "'";

         using (SqliteConnection connection = new(ConnectionString))
         {
            connection.Open();

            using SqliteCommand command = new(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               for (int i = 0; i < Count; i++)
               {
                  string column = "T" + i.ToString();
                  int ordinal = reader.GetOrdinal(column);

                  if (!reader.IsDBNull(ordinal))
                  {
                     count++;
                  }
                  else
                  {
                     break;
                  }
               }

               break;
            }
         }

         return count;
      }

      public string[] GetNullColumns(string id)
      {
         var columns = new List<string>();
         string sql = "SELECT * FROM Thumbnails WHERE ID='" + id + "'";

         using (SqliteConnection connection = new(ConnectionString))
         {
            connection.Open();

            using SqliteCommand command = new(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               foreach (var column in Columns.Keys)
               {
                  int ordinal = reader.GetOrdinal(column);

                  if (reader.IsDBNull(ordinal))
                  {
                     columns.Add(column);
                  }
               }

               break;
            }
         }

         return columns.ToArray();
      }

      public void SetJournalMode(string mode)
      {
         string sql = "PRAGMA journal_mode=";

         switch (mode.ToUpper())
         {
            case "DELETE":
               sql += "DELETE;";
               break;
            case "TRUNCATE":
               sql += "TRUNCATE;";
               break;
            case "PERSIST":
               sql += "PERSIST;";
               break;
            case "MEMORY":
               sql += "MEMORY;";
               break;
            case "WAL":
               sql += "WAL;";
               break;
            case "OFF":
               sql += "OFF;";
               break;
            default:
               throw new ArgumentException(string.Format("The {0} is an invalid journal mode!", mode));
         }

         using SqliteConnection connection = new(ConnectionString);

         connection.Open();

         using SqliteCommand command = new(sql, connection);

         command.ExecuteNonQuery();
      }

      public void Checkpoint(string argument = "TRUNCATE")
      {
         string sql = string.Format("PRAGMA wal_checkpoint{0};", (argument.Length > 0) ? "(PASSIVE)" : "");

         using SqliteConnection connection = new(ConnectionString);

         connection.Open();

         using SqliteCommand command = new(sql, connection);

         command.ExecuteNonQuery();
      }

      public void Vacuum()
      {
         string sql = "VACUUM;";

         using SqliteConnection connection = new(ConnectionString);

         connection.Open();

         using SqliteCommand command = new(sql, connection);

         command.ExecuteNonQuery();
      }

      #endregion

      #region Database Operations

      /// <summary>
      /// Updates the database layout creating tables and columns as necessary.
      /// </summary>
      private void UpdateDatabaseLayout()
      {
         // Enable the WAL (Write-Ahead Logging) journaling mode
         SetJournalMode("WAL");

         // Create the Thumbnails table
         if (!TableExists("Thumbnails"))
         {
            CreateThumbnailsTable();
         }

         // Add the ID column
         if (!ColumnExists("Thumbnails", "ID"))
         {
            AddColumn("Thumbnails", "ID", "TEXT PRIMARY KEY NOT NULL");
         }

         // Add the thumbnails columns as configured
         var formats = new[]
         {
            _settings.CurrentValue.Thumbnails.Video,
            _settings.CurrentValue.Thumbnails.Photo,
            _settings.CurrentValue.Thumbnails.Audio
         };

         foreach (var items in formats)
         {
            foreach (var item in items)
            {
               if (item.Value.Count > 0 && !item.Value.Sprite)
               {
                  for (int i = 0; i < item.Value.Count; i++)
                  {
                     string column = item.Key.ToUpper() + i.ToString();

                     if (!ColumnExists("Thumbnails", column))
                     {
                        AddColumn("Thumbnails", column, "BLOB");
                     }
                  }
               }
               else
               {
                  string column = item.Key.ToUpper();

                  if (!ColumnExists("Thumbnails", column))
                  {
                     AddColumn("Thumbnails", column, "BLOB");
                  }
               }
            }
         }
      }

      /// <summary>
      /// Checks whether or not a table exists in the Thumbnails Database.
      /// </summary>
      /// <param name="table">The table name.</param>
      /// <returns><c>true</c> if it already exists and <c>false</c> otherwise.</returns>
      private bool TableExists(string table)
      {
         string sql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = '" + table + "'";

         using SqliteConnection connection = new(ConnectionString);

         connection.Open();

         using SqliteCommand command = new(sql, connection);
         using SqliteDataReader reader = command.ExecuteReader();

         return reader.HasRows;
      }

      /// <summary>
      /// Checks whether or not a column exists in the given table of the Thumbnails Database.
      /// </summary>
      /// <param name="table">The table name.</param>
      /// <param name="column">The column name.</param>
      /// <returns><c>true</c> if it already exists and <c>false</c> otherwise.</returns>
      private bool ColumnExists(string table, string column)
      {
         string sql = "PRAGMA table_info( " + table + " )";

         using SqliteConnection connection = new(ConnectionString);

         connection.Open();

         using SqliteCommand command = new(sql, connection);
         using SqliteDataReader reader = command.ExecuteReader();

         while (reader.Read())
         {
            var name = reader["name"];

            if (name != null && name.ToString()!.ToUpper().Equals(column.ToUpper()))
            {
               return true;
            }
         }

         return false;
      }

      /// <summary>
      /// Checks whether a row with the specified value for a specific column exists.
      /// </summary>
      /// <param name="table">The table name.</param>
      /// <param name="column">The column name.</param>
      /// <param name="value">The value of the column.</param>
      /// <returns><c>true</c> if the row exists and <c>false</c>< otherwise./returns>
      private bool RowExists(string table, string column, string value)
      {
         string sql = "SELECT COUNT(*) FROM " + table + " WHERE " + column + "='" + value + "'";

         using SqliteConnection connection = new(ConnectionString);
         connection.Open();

         using SqliteCommand command = new(sql, connection);
         bool result = (Convert.ToInt32(command.ExecuteScalar()) > 0);

         return result;
      }

      /// <summary>
      /// Retrieves all or the column names and their types from a given table.
      /// </summary>
      /// <param name="table">The table name.</param>
      /// <returns></returns>
      private Dictionary<string, string> GetColumns(string table)
      {
         Dictionary<string, string> columns = [];
         string sql = "PRAGMA table_info( " + table + " )";

         using (SqliteConnection connection = new(ConnectionString))
         {
            connection.Open();

            using SqliteCommand command = new(sql, connection);
            using SqliteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
               var name = reader["name"];
               var type = reader["type"];

               if (name != null && type != null)
               {
                  columns.Add(name.ToString()!.ToUpper(), type.ToString()!.ToUpper());
               }
               else
               {
                  throw new InvalidOperationException("Table column name or type is null.");
               }
            }
         }

         return columns;
      }

      /// <summary>
      /// Creates the Thumbnails table.
      /// </summary>
      private void CreateThumbnailsTable()
      {
         string sql = "CREATE TABLE Thumbnails (ID text primary key not null)";

         using SqliteConnection connection = new(ConnectionString);
         connection.Open();

         using SqliteCommand command = new(sql, connection);
         command.ExecuteNonQuery();
      }

      /// <summary>
      /// Adds a column of the specified type to the specified table.
      /// </summary>
      /// <param name="table">The table name to modify.</param>
      /// <param name="column">The column name to add to the table.</param>
      /// <param name="type">The type column type to add to the table.</param>
      private void AddColumn(string table, string column, string type)
      {
         string sql = "ALTER TABLE " + table + " ADD COLUMN " + column + " " + type;

         using SqliteConnection connection = new(ConnectionString);
         connection.Open();

         using SqliteCommand command = new(sql, connection);
         command.ExecuteNonQuery();
      }

      private int AddRow(string table, string column, string value)
      {
         string sql = "INSERT INTO " + table + " (" + column + ") VALUES (@" + column + ")";

         using SqliteConnection connection = new(ConnectionString);
         connection.Open();

         using SqliteCommand command = new(sql, connection);

         command.Parameters.Add("@" + column, SqliteType.Text).Value = value;

         return command.ExecuteNonQuery();
      }

      private int DeleteRow(string table, string column, string value)
      {
         string sql = "DELETE FROM " + table + " WHERE " + column + "='" + value + "'";

         using SqliteConnection connection = new(ConnectionString);
         connection.Open();

         using SqliteCommand command = new(sql, connection);

         return command.ExecuteNonQuery();
      }

      #endregion
   }
}
