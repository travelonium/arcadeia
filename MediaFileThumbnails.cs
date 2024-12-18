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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Arcadeia
{
   public class MediaFileThumbnails(IThumbnailsDatabase database, string id)
   {
      private readonly IThumbnailsDatabase _database = database;

      private readonly string _id = id;

      /// <summary>
      /// Get or Set a specific thumbnail related to the current file using the provided index.
      /// </summary>
      /// <param name="index">The thumbnail index.</param>
      /// <returns>
      /// The thumbnail image data.
      /// </returns>
      public byte[] this[int index]
      {
         get
         {
            byte[] thumbnail = _database.GetThumbnail(_id, index);

            return thumbnail;
         }

         set => _database.SetThumbnail(_id, index, ref value);
      }

      /// <summary>
      /// Get or Set a specific thumbnail related to the current file using the provided label.
      /// </summary>
      /// <param name="label">The thumbnail label.</param>
      /// <returns>
      /// The thumbnail image data.
      /// </returns>
      public byte[] this[string label]
      {
         get
         {
            byte[] thumbnail = _database.GetThumbnail(_id, label);

            return thumbnail;
         }

         set => _database.SetThumbnail(_id, label, ref value);
      }

      /// <summary>
      /// Gets the total number of existing thumbnails in existence for this particular file.
      /// </summary>
      /// <value>
      /// The number of thumbnails available.
      /// </value>
      public int Count
      {
         get
         {
            return _database.GetThumbnailsCount(_id);
         }
      }

      /// <summary>
      /// Whether or not the row for the current file exists which means that it has been initialized.
      /// </summary>
      public bool Initialized
      {
         get
         {
            return _database.Exists(_id);
         }
      }

      /// <summary>
      /// Retrieve a list of all the null columns of the row.
      /// </summary>
      public string[] NullColumns
      {
         get
         {
            return _database.GetNullColumns(_id);
         }
      }

      public async Task<byte[]> GetAsync(int index, CancellationToken cancellationToken)
      {
         byte[] thumbnail = await _database.GetThumbnailAsync(_id, index, cancellationToken);

         return thumbnail;
      }

      /// <summary>
      /// Initialize the record for the current file.
      /// </summary>
      public void Initialize()
      {
         _database.Create(_id);
      }

      public int DeleteAll()
      {
         return _database.DeleteThumbnails(_id);
      }
   }
}
