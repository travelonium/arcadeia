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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SolrNet;

namespace Arcadeia.Solr
{
   public interface ISolrIndexService<T>
   {
      SolrQueryResults<T> Get(ISolrQuery query, ICollection<SortOrder> orders);
      SolrQueryResults<T> Get(string field, string value);
      SolrQueryResults<T> Get(ISolrQuery query);
      SolrQueryResults<T> Get(string query);
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
