using Nest;

namespace DataAPI.Services
{
    public interface ISearchService
    {
        Task<object> SearchAsync(string query);
        Task<object> SearchEventsAsync(string eventType, int? matchId);
    }

    public class SearchService : ISearchService
    {
        private readonly IElasticClient _elasticClient;
        private readonly ILogger<SearchService> _logger;

        public SearchService(IElasticClient elasticClient, ILogger<SearchService> logger)
        {
            _elasticClient = elasticClient;
            _logger = logger;
        }

        public async Task<object> SearchAsync(string query)
        {
            try
            {
                var searchResponse = await _elasticClient.SearchAsync<Common.Models.SportEvent>(s => s
                    .Query(q => q
                        .MultiMatch(mm => mm
                            .Query(query)
                            .Fields(f => f
                                .Field(e => e.Description)
                                .Field(e => e.Type)
                            )
                        )
                    )
                    .Size(50)
                );

                if (!searchResponse.IsValid)
                {
                    _logger.LogError("Elasticsearch search error: {Error}", searchResponse.DebugInformation);
                    return new { error = "Search failed" };
                }

                return searchResponse.Documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching Elasticsearch");
                throw;
            }
        }

        public async Task<object> SearchEventsAsync(string eventType, int? matchId)
        {
            try
            {
                var searchResponse = await _elasticClient.SearchAsync<Common.Models.SportEvent>(s =>
                {
                    var query = s.Query(q => q.Bool(b =>
                    {
                        var must = new List<Func<QueryContainerDescriptor<Common.Models.SportEvent>, QueryContainer>>();

                        if (!string.IsNullOrEmpty(eventType))
                        {
                            must.Add(m => m.Term(t => t.Field(f => f.Type).Value(eventType)));
                        }

                        if (matchId.HasValue)
                        {
                            must.Add(m => m.Term(t => t.Field(f => f.MatchId).Value(matchId.Value)));
                        }

                        return b.Must(must.ToArray());
                    }));

                    return query.Size(100).Sort(sort => sort.Descending(e => e.Timestamp));
                });

                if (!searchResponse.IsValid)
                {
                    _logger.LogError("Elasticsearch search error: {Error}", searchResponse.DebugInformation);
                    return new { error = "Search failed" };
                }

                return searchResponse.Documents;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                throw;
            }
        }
    }
}
