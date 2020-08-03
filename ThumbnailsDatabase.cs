using System;
using System.IO;
using System.Threading;
using System.Data.SQLite;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace MediaCurator
{
   class ThumbnailsDatabase : IThumbnailsDatabase
   {
      private readonly IConfiguration _configuration;

      private readonly ILogger<ThumbnailsDatabase> _logger;

      private Lazy<string> _fullPath => new Lazy<string>(_configuration["ThumbnailsDatabase:Path"] + Platform.Separator.Path + _configuration["ThumbnailsDatabase:Name"]);

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

      private Lazy<int> _maximum => new Lazy<int>(_configuration.GetValue<int>("ThumbnailsDatabase:Maximum"));

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
            // Create the new database file
            SQLiteConnection.CreateFile(FullPath);

            _logger.LogInformation("Thumbnails Database Created: " + FullPath);
         }

         // Update the database layout if needed.
         UpdateDatabaseLayout();
      }

      /// <summary>
      /// Finalizes an instance of the <see cref="ThumbnailsDatabase"/> class.
      /// </summary>
      ~ThumbnailsDatabase()
      {
      }

      #region Interface

      public void SetThumbnail(string id, int index, ref byte[] data)
      {
         if (!RowExists("Thumbnails", "Id", id))
         {
            AddRow("Thumbnails", "Id", id);
         }

         string column = "T" + index.ToString();
         string sql = "UPDATE Thumbnails SET " + column + "= @" + column + " WHERE Id='" + id + "'";

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);
         command.Parameters.Add("@" + column, System.Data.DbType.Binary).Value = data;
         command.ExecuteNonQuery();
      }

      public byte[] GetThumbnail(string id, int index)
      {
         byte[] thumbnail = { };
         string column = "T" + index.ToString();
         string sql = "SELECT " + column + " FROM Thumbnails WHERE Id='" + id + "'";

         using (SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;"))
         {
            connection.Open();

            using SQLiteCommand command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               object blob = reader[column];

               if (blob != null)
               {
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
         byte[] thumbnail = { };
         string column = "T" + index.ToString();
         string sql = "SELECT " + column + " FROM Thumbnails WHERE Id='" + id + "'";

         using (SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;"))
         {
            await connection.OpenAsync(cancellationToken);

            using SQLiteCommand command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
               object blob = reader[column];

               if (blob != null)
               {
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

      public List<byte[]> GetThumbnails(string id)
      {
         List<byte[]> thumbnails = new List<byte[]>();
         string sql = "SELECT * FROM Thumbnails WHERE Id='" + id + "'";

         using (SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;"))
         {
            connection.Open();

            using SQLiteCommand command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               for (int i = 0; i < Maximum; i++)
               {
                  string column = "T" + i.ToString();

                  object blob = reader[column];

                  if (blob != null)
                  {
                     if (blob.GetType() == typeof(byte[]))
                     {
                        thumbnails.Add((byte[])blob);
                     }
                  }
               }

               break;
            }
         }

         return thumbnails;
      }

      public async Task<List<byte[]>> GetThumbnailsAsync(string id, CancellationToken cancellationToken)
      {
         List<byte[]> thumbnails = new List<byte[]>();
         string sql = "SELECT * FROM Thumbnails WHERE Id='" + id + "'";

         using (SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;"))
         {
            await connection.OpenAsync(cancellationToken);

            using SQLiteCommand command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
               for (int i = 0; i < Maximum; i++)
               {
                  string column = "T" + i.ToString();

                  object blob = reader[column];

                  if (blob != null)
                  {
                     if (blob.GetType() == typeof(byte[]))
                     {
                        thumbnails.Add((byte[])blob);
                     }
                  }
               }

               break;
            }
         }

         return thumbnails;
      }

      public int DeleteThumbnails(string id)
      {
         return DeleteRow("Thumbnails", "Id", id);
      }

      public int GetThumbnailsCount(string id)
      {
         int count = 0;
         string sql = "SELECT * FROM Thumbnails WHERE Id='" + id + "'";

         using (SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;"))
         {
            connection.Open();

            using SQLiteCommand command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               for (int i = 0; i < Maximum; i++)
               {
                  string column = "T" + i.ToString();

                  object blob = reader[column];

                  if (blob != null)
                  {
                     if (blob.GetType() == typeof(byte[]))
                     {
                        count++;
                     }
                  }
               }

               break;
            }
         }

         return count;
      }

      public List<string> GetRowIdsList()
      {
         List<string> ids = new List<string>();
         string sql = "SELECT Id FROM Thumbnails";

         using (SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;"))
         {
            connection.Open();

            using SQLiteCommand command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
               object id = reader["Id"];

               if (id != null)
               {
                  if (id.GetType() == typeof(string))
                  {
                     ids.Add((string)id);
                  }
               }
            }
         }

         return ids;
      }

      public void Vacuum()
      {
         string sql = "VACUUM;";

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);

         command.ExecuteNonQuery();
      }

      #endregion

      #region Database Operations

      /// <summary>
      /// Updates the database layout creating tables and columns as necessary.
      /// </summary>
      private void UpdateDatabaseLayout()
      {
         // Create the Thumbnails table
         if (!TableExists("Thumbnails"))
         {
            CreateThumbnailsTable();
         }

         // Add the Id column
         if (!ColumnExists("Thumbnails", "Id"))
         {
            AddColumn("Thumbnails", "Id", "TEXT PRIMARY KEY NOT NULL");
         }

         // Add the T column
         if (!ColumnExists("Thumbnails", "T"))
         {
            AddColumn("Thumbnails", "T", "UNSIGNED BIG INT");
         }

         // Add the individual thumbnail columns
         for (int i = 0; i < _configuration.GetValue<int>("ThumbnailsDatabase:Maximum"); i++)
         {
            string column = "T" + i.ToString();

            if (!ColumnExists("Thumbnails", column))
            {
               AddColumn("Thumbnails", column, "BLOB");
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

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);
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

         using (SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;"))
         {
            connection.Open();

            using SQLiteCommand command = new SQLiteCommand(sql, connection);
            using SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
               if (reader["name"].Equals(column))
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

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);
         bool result = (Convert.ToInt32(command.ExecuteScalar()) > 0);

         return result;
      }

      /// <summary>
      /// Creates the Thumbnails table.
      /// </summary>
      private void CreateThumbnailsTable()
      {
         string sql = "CREATE TABLE Thumbnails (Id text primary key not null)";

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);
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

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);
         command.ExecuteNonQuery();
      }

      private int AddRow(string table, string column, string value)
      {
         string sql = "INSERT INTO " + table + " (" + column + ") VALUES (@" + column + ")";

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);

         command.Parameters.Add("@" + column, System.Data.DbType.StringFixedLength).Value = value;

         return command.ExecuteNonQuery();
      }

      private int DeleteRow(string table, string column, string value)
      {
         string sql = "DELETE FROM " + table + " WHERE " + column + "='" + value + "'";

         using SQLiteConnection connection = new SQLiteConnection(@"Data Source=" + FullPath + ";Version=3;Pooling=True;Max Pool Size=100;");
         connection.Open();

         using SQLiteCommand command = new SQLiteCommand(sql, connection);

         return command.ExecuteNonQuery();
      }

      #endregion
   }
}
