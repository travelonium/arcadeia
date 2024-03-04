using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SolrNet;

namespace MediaCurator.Solr
{
   public interface ISolrIndexService<T>
   {
      SolrQueryResults<T> Get(string field, string value);
      SolrQueryResults<T> Get(ISolrQuery query);
      bool Add(T document);
      bool Update(T document);
      bool Delete(T document);
      bool Clear();
      bool ClearHistory();

      Task<bool> Ping();
      Task<bool> AddFieldType(string name, Dictionary<string, object> definition);
      Task<bool> FieldTypeExistsAsync(string name);
      Task<bool> AddField(string name, Dictionary<string, object> definition);
      Task<bool> FieldExistsAsync(string name);

      void Initialize();
   }
}
