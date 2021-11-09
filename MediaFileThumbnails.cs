using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace MediaCurator
{
   public class MediaFileThumbnails
   {
      private IThumbnailsDatabase _database { get; }

      private string _id
      {
         get;
         set;
      }

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

      #region Constructors

      public MediaFileThumbnails(IThumbnailsDatabase database, string id)
      {
         // Associate the current instance with the Id of the owner.
         _id = id;

         // Associate the instance with the thumbnails database.
         _database = database;
      }

      /// <summary>
      /// Parameterless constructor used for deserialization of JSON values.
      /// </summary>
      public MediaFileThumbnails()
      {

      }

      ~MediaFileThumbnails()
      {

      }

      #endregion // Constructors

      public async Task<byte[]> GetAsync(int index, CancellationToken cancellationToken)
      {
         byte[] thumbnail = await _database.GetThumbnailAsync(_id, index, cancellationToken);
         return thumbnail;
      }

      public int DeleteAll()
      {
         return _database.DeleteThumbnails(_id);
      }
   }
}
