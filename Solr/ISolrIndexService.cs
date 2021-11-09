using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaCurator.Solr
{
   public interface ISolrIndexService<T>
   {
      bool Add(T document);
      bool Update(T document);
      bool Delete(T document);

      Task<bool> Ping();
      Task<bool> AddFieldType(string name, Dictionary<string, object> definition);
      Task<bool> FieldTypeExistsAsync(string name);
      Task<bool> AddField(string name, Dictionary<string, object> definition);
      Task<bool> FieldExistsAsync(string name);

      void Initialize();
   }
}
