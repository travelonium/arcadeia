using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace MediaCurator
{
   /// <summary>
   /// The supported media container type extensions.
   /// </summary>
   public class MediaContainerTypeExtensions
   {
      public IConfiguration Configuration { get; }

      private Dictionary<MediaContainerType, List<string>> _database = new Dictionary<MediaContainerType, List<string>>();

      public List<string> this[MediaContainerType type]
      {
         get
         {
            return _database[type];
         }
      }

      public MediaContainerTypeExtensions(IConfiguration configuration)
      {
         Configuration = configuration;

         _database[MediaContainerType.Audio] = Configuration.GetSection("SupportedExtensions:Audio").Get<List<string>>();
         _database[MediaContainerType.Video] = Configuration.GetSection("SupportedExtensions:Video").Get<List<string>>();
         _database[MediaContainerType.Photo] = Configuration.GetSection("SupportedExtensions:Photo").Get<List<string>>();
      }
   }
}
