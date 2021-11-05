using SolrNet;
using SolrNet.Exceptions;
using Microsoft.Extensions.Logging;

namespace MediaCurator.Solr
{
   public class SolrIndexService<T, TSolrOperations> : ISolrIndexService<T> where TSolrOperations : ISolrOperations<T>
	{
		private readonly TSolrOperations _solr;

		private readonly ILogger<SolrIndexService<T, TSolrOperations>> _logger;

		public SolrIndexService(ISolrOperations<T> solr,
										ILogger<SolrIndexService<T, TSolrOperations>> logger)
		{
			_logger = logger;
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
   }
}
