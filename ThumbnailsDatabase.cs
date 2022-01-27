using System;
using System.IO;
using System.Threading;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace MediaCurator
{
   class ThumbnailsDatabase : IThumbnailsDatabase
   {
      private readonly IConfiguration _configuration;

      private readonly ILogger<ThumbnailsDatabase> _logger;

      private Lazy<string> _path => new(_configuration["Thumbnails:Database:Path"]);

      private Lazy<string> _fullPath => new(_configuration["Thumbnails:Database:Path"] + Platform.Separator.Path + _configuration["Thumbnails:Database:Name"]);

      private Dictionary<string, string> _columns = new();

      private Lazy<string> _connectionString => new(new SQLiteConnectionStringBuilder
      {
         Version = 3,
         Pooling = true,
         DataSource = FullPath,
         BusyTimeout = 5000
      }.ToString());

      private Lazy<int> _maximum => new(() =>
      {
         int maximum = 0;

         foreach (var name in new string[] { "Thumbnails:Video", "Thumbnails:Photo", "Thumbnails:Audio" })
         {
            var section = _configuration.GetSection(name);

            if (section.Exists())
            {
               foreach (var item in section.Get<Dictionary<string, Dictionary<string, int>>>())
               {
                  if (item.Value.ContainsKey("Count"))
                  {
                     maximum = Math.Max(item.Value["Count"], maximum);
                  }
               }
            }
         }

         return maximum;
      });

      /// <summary>
      /// The directory in which where the database file is to be found or created.
      /// </summary>
      public string Path
      {
         get
         {
            return _path.Value;
         }
      }

      /// <summary>
      /// The full path to the database file.
      /// </summary>
      public string FullPath
      {
         get
         {
            return _fullPath.Value;
         }
      }

      /// <summary>
      /// The maximum count of thumbnails the database is able to store.
      /// </summary>
      public int Maximum
      {
         get
         {
            return _maximum.Value;
         }
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="ThumbnailsDatabase"/> class.
      /// </summary>
      public ThumbnailsDatabase(IConfiguration configuration, ILogger<ThumbnailsDatabase> logger)
      {
         _logger = logger;
         _configuration = configuration;

         // Check if the Thumbnails Database file already exists.
         if (!File.Exists(FullPath))
         {
            try
            {
               // Create the hosting directory
               Directory.CreateDirectory(Path);

               // Create the new database file
               SQLiteConnection.CreateFile(FullPath);

               _logger.LogInformation("Thumbnails Database Created: {}", FullPath);
            }
            catch (Exception e)
            {
               _logger.LogError("Thumbnails Database Creation Failed! Becasue: {}", e.Message);
            }
         }

         // Update the database layout if needed.
         UpdateDatabaseLayout();

         // Retrieve and store the list of all the columns
         _columns = GetColumns("Thumbnails");
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

         using SQLiteConnection connection = new(_connectionString.Value);
         connection.Open();

         using SQLiteCommand command = new(sql, connection);
         command.Parameters.Add("@" + column, System.Data.DbType.Binary).Value = data;
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

         using SQLiteConnection connection = new(_connectionString.Value);
         connection.Open();

         using SQLiteCommand command = new(sql, connection);
         command.Parameters.Add("@" + column, System.Data.DbType.Binary).Value = data;
         command.ExecuteNonQuery();
      }

      public byte[] GetThumbnail(string id, string label)
      {
         string column = label.ToUpper();
         byte[] thumbnail = Array.Empty<byte>();
         string sql = "SELECT " + column + " FROM Thumbnails WHERE ID='" + id + "'";

         if (!_columns.ContainsKey(column)) return thumbnail;

         using (SQLiteConnection connection = new(_connectionString.Value))
         {
            connection.Open();

            using SQLiteCommand command = new(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               object blob = reader[column];

               if ((blob != null) && (blob.GetType() == typeof(byte[])))
               {
                  thumbnail = (byte[])blob;

                  break;
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

         if (!_columns.ContainsKey(column)) return thumbnail;

         using (SQLiteConnection connection = new(_connectionString.Value))
         {
            await connection.OpenAsync(cancellationToken);

            using SQLiteCommand command = new(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
               object blob = reader[column];

               if ((blob != null) && (blob.GetType() == typeof(byte[])))
               {
                  thumbnail = (byte[])blob;

                  break;
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

         using (SQLiteConnection connection = new(_connectionString.Value))
         {
            connection.Open();

            using SQLiteCommand command = new(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               for (int i = 0; i < Maximum; i++)
               {
                  string column = "T" + i.ToString();

                  object blob = reader[column];

                  if ((blob != null) && (blob.GetType() == typeof(byte[])))
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

      public void SetJournalMode(SQLiteJournalModeEnum mode)
      {
         string sql = "PRAGMA journal_mode=";

         switch (mode)
         {
            case SQLiteJournalModeEnum.Delete:
               sql += "DELETE;";
               break;
            case SQLiteJournalModeEnum.Truncate:
               sql += "TRUNCATE;";
               break;
            case SQLiteJournalModeEnum.Persist:
               sql += "PERSIST;";
               break;
            case SQLiteJournalModeEnum.Memory:
               sql += "MEMORY;";
               break;
            case SQLiteJournalModeEnum.Wal:
               sql += "WAL;";
               break;
            case SQLiteJournalModeEnum.Off:
               sql += "OFF;";
               break;
            case SQLiteJournalModeEnum.Default:
            default:
               return;
         }

         using SQLiteConnection connection = new(_connectionString.Value);

         connection.Open();

         using SQLiteCommand command = new(sql, connection);

         command.ExecuteNonQuery();
      }

      public void Checkpoint(string argument = "TRUNCATE")
      {
         string sql = String.Format("PRAGMA wal_checkpoint{0};", (argument.Length > 0) ? "(PASSIVE)" : "");

         using SQLiteConnection connection = new(_connectionString.Value);

         connection.Open();

         using SQLiteCommand command = new(sql, connection);

         command.ExecuteNonQuery();
      }

      public void Vacuum()
      {
         string sql = "VACUUM;";

         using SQLiteConnection connection = new(_connectionString.Value);

         connection.Open();

         using SQLiteCommand command = new(sql, connection);

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
         SetJournalMode(SQLiteJournalModeEnum.Wal);

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
         foreach (var name in new string[] { "Thumbnails:Video", "Thumbnails:Photo", "Thumbnails:Audio" })
         {
            var section = _configuration.GetSection(name);

            if (section.Exists())
            {
               foreach (var item in section.Get<Dictionary<string, Dictionary<string, int>>>())
               {
                  if (item.Value.ContainsKey("Count"))
                  {
                     for (int i = 0; i < item.Value["Count"]; i++)
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
      }

      /// <summary>
      /// Checks whether or not a table exists in the Thumbnails Database.
      /// </summary>
      /// <param name="table">The table name.</param>
      /// <returns><c>true</c> if it already exists and <c>false</c> otherwise.</returns>
      private bool TableExists(string table)
      {
         string sql = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = '" + table + "'";

         using SQLiteConnection connection = new(_connectionString.Value);

         connection.Open();

         using SQLiteCommand command = new(sql, connection);
         using SQLiteDataReader reader = command.ExecuteReader();

         bool result = ((reader.StepCount > 0));

         return result;
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

         using (SQLiteConnection connection = new(_connectionString.Value))
         {
            connection.Open();

            using SQLiteCommand command = new(sql, connection);
            using SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
               if (reader["name"].ToString().ToUpper().Equals(column.ToUpper()))
               {
                  return true;
               }
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

         using SQLiteConnection connection = new(_connectionString.Value);
         connection.Open();

         using SQLiteCommand command = new(sql, connection);
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
         Dictionary<string, string> columns = new();
         string sql = "PRAGMA table_info( " + table + " )";

         using (SQLiteConnection connection = new(_connectionString.Value))
         {
            connection.Open();

            using SQLiteCommand command = new(sql, connection);
            using SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
               columns.Add(reader["name"].ToString().ToUpper(), reader["type"].ToString().ToUpper());
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

         using SQLiteConnection connection = new(_connectionString.Value);
         connection.Open();

         using SQLiteCommand command = new(sql, connection);
         command.ExecuteNonQuery();
      }

      /// <summary>
      /// Adds a column of the specified type to the specified table.
      /// </summary>
      /// <param name="table">The table name to modify.</param>
      /// <param name="column">The column name to add to the table.</param>
      /// <param name="type">The type colmn type to add to the table.</param>
      private void AddColumn(string table, string column, string type)
      {
         string sql = "ALTER TABLE " + table + " ADD COLUMN " + column + " " + type;

         using SQLiteConnection connection = new(_connectionString.Value);
         connection.Open();

         using SQLiteCommand command = new(sql, connection);
         command.ExecuteNonQuery();
      }

      private int AddRow(string table, string column, string value)
      {
         string sql = "INSERT INTO " + table + " (" + column + ") VALUES (@" + column + ")";

         using SQLiteConnection connection = new(_connectionString.Value);
         connection.Open();

         using SQLiteCommand command = new(sql, connection);

         command.Parameters.Add("@" + column, System.Data.DbType.StringFixedLength).Value = value;

         return command.ExecuteNonQuery();
      }

      private int DeleteRow(string table, string column, string value)
      {
         string sql = "DELETE FROM " + table + " WHERE " + column + "='" + value + "'";

         using SQLiteConnection connection = new(_connectionString.Value);
         connection.Open();

         using SQLiteCommand command = new(sql, connection);

         return command.ExecuteNonQuery();
      }

      #endregion
   }
}
