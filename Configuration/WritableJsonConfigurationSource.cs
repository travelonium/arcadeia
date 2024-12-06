using Microsoft.Extensions.Configuration.Json;

namespace MediaCurator.Configuration
{
   public class WritableJsonConfigurationSource : JsonConfigurationSource
   {
      public override IConfigurationProvider Build(IConfigurationBuilder builder)
      {
         EnsureDefaults(builder);

         return new WritableJsonConfigurationProvider(this);
      }
   }
}
