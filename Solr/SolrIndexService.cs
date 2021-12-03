using SolrNet;
using SolrNet.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;

namespace MediaCurator.Solr
{
   public class SolrIndexService<T, TSolrOperations> : ISolrIndexService<T> where TSolrOperations : ISolrOperations<T>
   {
		private readonly TSolrOperations _solr;

		private readonly IConfiguration _configuration;

		private readonly IHttpClientFactory _factory;

		private readonly ILogger<SolrIndexService<T, TSolrOperations>> _logger;

		private readonly Dictionary<string, Dictionary<string, object>> _types = new Dictionary<string, Dictionary<string, object>>
		{
			{
				"text_ngram", new Dictionary<string, object>
				{
					{ "name", "text_ngram" },
					{ "class", "solr.TextField" },
					{ "positionIncrementGap", "100" },
					{ "multiValued", true },
					{
						"indexAnalyzer", new Dictionary<string, object>
						{
							{
								"tokenizer", new Dictionary<string, object>
								{
									{ "class", "solr.StandardTokenizerFactory" }
								}
							},
							{
								"filters", new List<Dictionary<string, object>>
								{
									new Dictionary<string, object>
									{
										{ "class", "solr.StopFilterFactory" },
										{ "words", "stopwords.txt" },
										{ "ignoreCase", "true" }
									},
									new Dictionary<string, object>
									{
										{ "class", "solr.NGramFilterFactory" },
										{ "minGramSize", "1" },
										{ "maxGramSize", "50" }
									},
									new Dictionary<string, object>
									{
										{ "class", "solr.LowerCaseFilterFactory" },
									}
								}
							}
						}
					},
					{
						"queryAnalyzer", new Dictionary<string, object>
						{
							{
								"tokenizer", new Dictionary<string, object>
								{
									{ "class", "solr.StandardTokenizerFactory" }
								}
							},
							{
								"filters", new List<Dictionary<string, object>>
								{
									new Dictionary<string, object>
									{
										{ "class", "solr.StopFilterFactory" },
										{ "words", "stopwords.txt" },
										{ "ignoreCase", "true" }
									},
									new Dictionary<string, object>
									{
										{ "class", "solr.SynonymGraphFilterFactory" },
										{ "expand", "true" },
										{ "ignoreCase", "true" },
										{ "synonyms", "synonyms.txt" }
									},
									new Dictionary<string, object>
									{
										{ "class", "solr.LowerCaseFilterFactory" },
									}
								}
							}
						}
					}
				}
			}
		};

		private readonly Dictionary<string, Dictionary<string, object>> _fields = new Dictionary<string, Dictionary<string, object>>
		{
			{	"name", new Dictionary<string, object>
				{
					{ "name", "name" },
					{ "type", "text_ngram" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "type", new Dictionary<string, object>
				{
					{ "name", "type" },
					{ "type", "text_ngram" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "path", new Dictionary<string, object>
				{
					{ "name", "path" },
					{ "type", "string" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "fullPath", new Dictionary<string, object>
				{
					{ "name", "fullPath" },
					{ "type", "text_ngram" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "flags", new Dictionary<string, object>
				{
					{ "name", "flags" },
					{ "type", "text_general" },
					{ "multiValued", true },
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

		public async Task<bool> Ping()
      {
         try
         {
				HttpClient client = _factory.CreateClient();
				HttpResponseMessage response = await client.GetAsync(URL + "/admin/ping");
				return response.IsSuccessStatusCode;
			}
			catch (System.Exception)
         {
				return false;
         }
		}

		public async Task<bool> FieldTypeExistsAsync(string name)
		{
			HttpClient client = _factory.CreateClient();
			HttpResponseMessage response = await client.GetAsync(URL + "/schema/fieldtypes/" + name);
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> AddFieldType(string name, Dictionary<string, object> definition)
		{
			HttpClient client = _factory.CreateClient();
			Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>
			{
				{ "add-field-type",  definition }
			};
			HttpResponseMessage response = await client.PostAsync(URL + "/schema", new StringContent(JsonSerializer.Serialize(data)));
			return response.IsSuccessStatusCode;
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
			uint retries = 10;

			_logger.LogInformation("Waiting For Solr Server...");

			while (true)
			{
				if (Ping().Result)
            {
					break;
            }

            if (--retries == 0)
            {
					_logger.LogError("Timeout While Waiting For Solr Server!");

					break;
            }

				Thread.Sleep(6000);
			}

			try
         {
				// Ensure the required field types are defined and define them otherwise.
				foreach (var key in _types.Keys)
				{
					if (!FieldTypeExistsAsync(key).Result)
					{
						_logger.LogInformation("Schema Field Type Not Found: {}", key);

						if (AddFieldType(key, _types[key]).Result)
						{
							_logger.LogInformation("Schema Field Type Added: {}", key);
						}
						else
						{
							_logger.LogError("Failed To Add Schema Field Type: {}", key);
						}
					}
				}

				// Ensure the required fields are defined and define them otherwise.
				foreach (var key in _fields.Keys)
				{
					if (!FieldExistsAsync(key).Result)
					{
						_logger.LogInformation("Schema Field Not Found: {}", key);

						if (AddField(key, _fields[key]).Result)
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
			catch (System.Exception e)
         {
				_logger.LogError("Solr Connection Error: {}" + e.Message);

				throw;
			}
		}
   }
}
