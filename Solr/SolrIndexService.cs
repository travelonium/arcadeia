using SolrNet;
using SolrNet.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;

namespace MediaCurator.Solr
{
   public class SolrIndexService<T, TSolrOperations> : ISolrIndexService<T> where TSolrOperations : ISolrOperations<T>
   {
		private readonly TSolrOperations Solr;

		private readonly IConfiguration Configuration;

		private readonly IHttpClientFactory Factory;

		private readonly ILogger<SolrIndexService<T, TSolrOperations>> Logger;

		private readonly Dictionary<string, Dictionary<string, object>> Types = new()
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

		private readonly Dictionary<string, Dictionary<string, object>> Fields = new()
      {
			{
				"id", new Dictionary<string, object>
				{
					{ "name", "id" },
					{ "type", "string" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"name", new Dictionary<string, object>
				{
					{ "name", "name" },
					{ "type", "string" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"type", new Dictionary<string, object>
				{
					{ "name", "type" },
					{ "type", "string" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"parent", new Dictionary<string, object>
				{
					{ "name", "parent" },
					{ "type", "string" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"parentType", new Dictionary<string, object>
				{
					{ "name", "parentType" },
					{ "type", "string" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"description", new Dictionary<string, object>
				{
					{ "name", "description" },
					{ "type", "string" },
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
					{ "type", "string" },
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
			{
				"views", new Dictionary<string, object>
				{
					{ "name", "views" },
					{ "type", "plong" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"size", new Dictionary<string, object>
				{
					{ "name", "size" },
					{ "type", "plong" },
					{ "multiValued", false },
					{ "stored", true },
				}
			},
			{
				"dateAdded", new Dictionary<string, object>
				{
					{ "name", "dateAdded" },
					{ "type", "pdate" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"dateCreated", new Dictionary<string, object>
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
			{
				"dateTaken", new Dictionary<string, object>
				{
					{ "name", "dateTaken" },
					{ "type", "pdate" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{
				"contentType", new Dictionary<string, object>
				{
					{ "name", "contentType" },
					{ "type", "string" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
			{  "extension", new Dictionary<string, object>
				{
					{ "name", "extension" },
					{ "type", "string" },
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

		private readonly Dictionary<string, Dictionary<string, object>> DynamicFields = new()
		{
			{
				"*_ngram", new Dictionary<string, object>
				{
					{ "name", "*_ngram" },
					{ "type", "text_ngram" },
					{ "multiValued", false },
					{ "indexed", true },
					{ "stored", true },
				}
			},
		};

		private readonly Dictionary<string, Dictionary<string, object>> CopyFields = new()
		{
			{
				"name_ngram", new Dictionary<string, object>
				{
					{ "source", "name" },
					{ "dest", "name_ngram" },
				}
			},
			{
				"description_ngram", new Dictionary<string, object>
				{
					{ "source", "description" },
					{ "dest", "description_ngram" },
				}
			},
			{
				"path_ngram", new Dictionary<string, object>
				{
					{ "source", "path" },
					{ "dest", "path_ngram" },
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
				return Configuration.GetSection("Solr:URL").Get<string>();
			}
		}

		public SolrIndexService(ISolrOperations<T> solr,
										IHttpClientFactory factory,
										IConfiguration configuration,
										ILogger<SolrIndexService<T, TSolrOperations>> logger)
		{
			Logger = logger;
			Factory = factory;
         Configuration = configuration;
			Solr = (TSolrOperations) solr;
		}

		public SolrQueryResults<T> Get(string field, string value)
      {
			return Solr.Query(new SolrQueryByField(field, value));
      }

		public SolrQueryResults<T> Get(ISolrQuery query)
		{
			return Solr.Query(query);
		}

		public bool Add(T document)
		{
			return Update(document);
		}

		public bool Update(T document)
		{
			try
			{
				Solr.Add(document);
				Solr.Commit();

				return true;
			}
			catch (SolrNetException e)
			{
				Logger.LogError("Failed To Update, Because: {}", e.Message);

				return false;
			}
		}

		public bool Delete(T document)
		{
			try
			{
				Solr.Delete(document);
				Solr.Commit();

				return true;
			}
			catch (SolrNetException e)
			{
				Logger.LogError("Failed To Delete, Because: {}", e.Message);

				return false;
			}
		}

		public bool Clear()
      {
			try
			{
				Solr.Delete(SolrQuery.All);
				Solr.Commit();

				Logger.LogInformation("Solr Core Cleared!");

				return true;
			}
			catch (SolrNetException e)
			{
				Logger.LogError("Failed To Clear, Because: {}", e.Message);

				return false;
			}
		}

		public async Task<bool> Ping()
      {
         try
         {
				HttpClient client = Factory.CreateClient();
				HttpResponseMessage response = await client.GetAsync(URL + "/admin/ping");
				return response.IsSuccessStatusCode;
			}
			catch (System.Exception)
         {
				return false;
         }
		}

		public async Task<bool> AddFieldType(string name, Dictionary<string, object> definition)
		{
			HttpClient client = Factory.CreateClient();
			Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>
			{
				{ "add-field-type",  definition }
			};
			HttpResponseMessage response = await client.PostAsync(URL + "/schema", new StringContent(JsonSerializer.Serialize(data)));
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> AddField(string name, Dictionary<string, object> definition)
      {
			HttpClient client = Factory.CreateClient();
			Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>
			{
				{ "add-field",  definition }
			};
			HttpResponseMessage response = await client.PostAsync(URL + "/schema", new StringContent(JsonSerializer.Serialize(data)));
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> AddDynamicField(string name, Dictionary<string, object> definition)
		{
			HttpClient client = Factory.CreateClient();
			Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>
			{
				{ "add-dynamic-field",  definition }
			};
			HttpResponseMessage response = await client.PostAsync(URL + "/schema", new StringContent(JsonSerializer.Serialize(data)));
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> AddCopyField(string name, Dictionary<string, object> definition)
		{
			HttpClient client = Factory.CreateClient();
			Dictionary<string, Dictionary<string, object>> data = new Dictionary<string, Dictionary<string, object>>
			{
				{ "add-copy-field",  definition }
			};
			HttpResponseMessage response = await client.PostAsync(URL + "/schema", new StringContent(JsonSerializer.Serialize(data)));
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> FieldTypeExistsAsync(string name)
		{
			HttpClient client = Factory.CreateClient();
			HttpResponseMessage response = await client.GetAsync(URL + "/schema/fieldtypes/" + name);
			return response.IsSuccessStatusCode;
		}

		public async Task<bool> FieldExistsAsync(string name)
      {
			HttpClient client = Factory.CreateClient();
			HttpResponseMessage response = await client.GetAsync(URL + "/schema/");

			response.EnsureSuccessStatusCode();

			string content = await response.Content.ReadAsStringAsync();
			var json = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(content);
			var fields = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, object>>>(json["schema"]["fields"].ToString());

			foreach (var item in fields)
			{
				if (item["name"].ToString().Equals(name))
				{
					return true;
				}
			}

			return false;
		}

		public async Task<bool> DynamicFieldExistsAsync(string name)
		{
			HttpClient client = Factory.CreateClient();
			HttpResponseMessage response = await client.GetAsync(URL + "/schema/");

			response.EnsureSuccessStatusCode();

			string content = await response.Content.ReadAsStringAsync();
			var json = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(content);
			var dynamicFields = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, object>>>(json["schema"]["dynamicFields"].ToString());

			foreach (var item in dynamicFields)
			{
				if (item["name"].ToString().Equals(name))
				{
					return true;
				}
			}

			return false;
		}

		public async Task<bool> CopyFieldExistsAsync(string name)
		{
			HttpClient client = Factory.CreateClient();
			HttpResponseMessage response = await client.GetAsync(URL + "/schema/");

			response.EnsureSuccessStatusCode();

			string content = await response.Content.ReadAsStringAsync();
			var json = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(content);
			var copyFields = JsonSerializer.Deserialize<IEnumerable<Dictionary<string, object>>>(json["schema"]["copyFields"].ToString());

			foreach (var item in copyFields)
         {
				if (item["dest"].ToString().Equals(name))
				{
					return true;
				}
         }

			return false;
		}

		public void Initialize()
      {
			uint retries = 10;

			Logger.LogInformation("Waiting For Solr Server...");

			while (true)
			{
				if (Ping().Result)
            {
					break;
            }

            if (--retries == 0)
            {
					Logger.LogError("Timeout While Waiting For Solr Server!");

					break;
            }

				Thread.Sleep(6000);
			}

			try
         {
				// Ensure the required field types are defined and define them otherwise.
				foreach (var key in Types.Keys)
				{
					if (!FieldTypeExistsAsync(key).Result)
					{
						Logger.LogInformation("Schema Field Type Not Found: {}", key);

						if (AddFieldType(key, Types[key]).Result)
						{
							Logger.LogInformation("Schema Field Type Added: {}", key);
						}
						else
						{
							Logger.LogError("Failed To Add Schema Field Type: {}", key);
						}
					}
				}

				// Ensure the required fields are defined and define them otherwise.
				foreach (var key in Fields.Keys)
				{
					if (!FieldExistsAsync(key).Result)
					{
						Logger.LogInformation("Schema Field Not Found: {}", key);

						if (AddField(key, Fields[key]).Result)
						{
							Logger.LogInformation("Schema Field Added: {}", key);
						}
						else
						{
							Logger.LogError("Failed To Add Schema Field: {}", key);
						}
					}
				}

				// Ensure the required dynamic fields are defined and define them otherwise.
				foreach (var key in DynamicFields.Keys)
				{
					if (!DynamicFieldExistsAsync(key).Result)
					{
						Logger.LogInformation("Dynamic Schema Field Not Found: {}", key);

						if (AddDynamicField(key, DynamicFields[key]).Result)
						{
							Logger.LogInformation("Dynamic Schema Field Added: {}", key);
						}
						else
						{
							Logger.LogError("Failed To Add Dynamic Schema Field: {}", key);
						}
					}
				}

				// Ensure the required copy fields are defined and define them otherwise.
				foreach (var key in CopyFields.Keys)
				{
					if (!CopyFieldExistsAsync(key).Result)
					{
						Logger.LogInformation("Copy Schema Field Not Found: {}", key);

						if (AddCopyField(key, CopyFields[key]).Result)
						{
							Logger.LogInformation("Copy Schema Field Added: {}", key);
						}
						else
						{
							Logger.LogError("Failed To Add Copy Schema Field: {}", key);
						}
					}
				}

				Logger.LogInformation("Solr Index Service Initialized.");
			}
			catch (System.Exception e)
         {
				Logger.LogError("Solr Connection Error: {}", e.Message);

				throw;
			}
		}
   }
}
