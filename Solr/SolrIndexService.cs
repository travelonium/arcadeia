using SolrNet;
using SolrNet.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;

namespace MediaCurator.Solr
{
   public class SolrIndexService<T, TSolrOperations> : ISolrIndexService<T> where TSolrOperations : ISolrOperations<T>
   {
		private readonly TSolrOperations _solr;

		private readonly IConfiguration _configuration;

		private readonly IHttpClientFactory _factory;

		private readonly ILogger<SolrIndexService<T, TSolrOperations>> _logger;

		private readonly Dictionary<string, Dictionary<string, object>> _schema = new Dictionary<string, Dictionary<string, object>>
		{
			{  "name", new Dictionary<string, object>
				{
					{ "name", "name" },
					{ "type", "text_general" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "type", new Dictionary<string, object>
				{
					{ "name", "type" },
					{ "type", "text_general" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "fullPath", new Dictionary<string, object>
				{
					{ "name", "fullPath" },
					{ "type", "text_general" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "size", new Dictionary<string, object>
				{
					{ "name", "size" },
					{ "type", "plong" },
					{ "multiValued", false },
					{ "stored", true },
				}
			},
			{  "dateCreated", new Dictionary<string, object>
				{
					{ "name", "dateCreated" },
					{ "type", "pdate" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "dateModified", new Dictionary<string, object>
				{
					{ "name", "dateModified" },
					{ "type", "pdate" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "thumbnails", new Dictionary<string, object>
				{
					{ "name", "thumbnails" },
					{ "type", "plong" },
					{ "multiValued", false },
					{ "stored", true },
				}
			},
			{  "duration", new Dictionary<string, object>
				{
					{ "name", "duration" },
					{ "type", "pdouble" },
					{ "multiValued", false },
					{ "stored", true },
				}
			},
			{  "width", new Dictionary<string, object>
				{
					{ "name", "width" },
					{ "type", "plong" },
					{ "multiValued", false },
					{ "stored", true },
				}
			},
			{  "height", new Dictionary<string, object>
				{
					{ "name", "height" },
					{ "type", "plong" },
					{ "multiValued", false },
					{ "stored", true },
				}
			},
		};

		/// <summary>
		/// Returns the Solr server URL.
		/// </summary>
		public string URL
		{
			get
			{
				return _configuration.GetSection("Solr:URL").Get<string>();
			}
		}

		public SolrIndexService(ISolrOperations<T> solr,
										IHttpClientFactory factory,
										IConfiguration configuration,
										ILogger<SolrIndexService<T, TSolrOperations>> logger)
		{
			_logger = logger;
			_factory = factory;
			_configuration = configuration;
			_solr = (TSolrOperations)solr;
		}

		public bool Add(T document)
		{
			return Update(document);
		}

		public bool Update(T document)
		{
			try
			{
				_solr.Add(document);
				_solr.Commit();

				return true;
			}
			catch (SolrNetException e)
			{
				_logger.LogError("Failed To Update, Because: {}", e.Message);

				return false;
			}
		}

		public bool Delete(T document)
		{
			try
			{
				_solr.Delete(document);
				_solr.Commit();

				return true;
			}
			catch (SolrNetException e)
			{
				_logger.LogError("Failed To Delete, Because: {}", e.Message);

				return false;
			}
		}

		public async Task<bool> AddField(string name, Dictionary<string, object> definition)
      {
			HttpClient client = _factory.CreateClient();
			Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>
			{
				{ "add-field",  definition }
			};
			HttpResponseMessage response = await client.PostAsync(URL + "/schema", new StringContent(JsonSerializer.Serialize(data)));
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> FieldExistsAsync(string name)
      {
			HttpClient client = _factory.CreateClient();
         HttpResponseMessage response = await client.GetAsync(URL + "/schema/fields/" + name);
			return response.IsSuccessStatusCode;
      }

      public void Initialize()
      {
         foreach (var key in _schema.Keys)
         {
				if (!FieldExistsAsync(key).Result)
				{
					_logger.LogInformation("Schema Field Not Found: {}", key);

					if (AddField(key, _schema[key]).Result)
               {
						_logger.LogInformation("Schema Field Added: {}", key);
					}
					else
               {
						_logger.LogError("Failed To Add Schema Field: {}", key);
					}
				}
			}
		}
   }
}
