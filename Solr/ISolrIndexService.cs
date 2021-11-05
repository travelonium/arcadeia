using System;

namespace MediaCurator.Solr
{
   public interface ISolrIndexService<T>
   {
      bool Add(T document);
      bool Update(T document);
      bool Delete(T document);
   }
}
