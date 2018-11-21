using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Patros.ServiceStatus.Models;

namespace Patros.ServiceStatus.SeqPoller
{
    class SeqReportRequest
    {

    }

    class SeqReportStatistics
    {
        public int ScannedEventCount { get; set; }
        public int MatchingEventCount { get; set; }
        public bool UncachedSegmentsScanned { get; set; }
        public float ElapsedMilliseconds { get; set; }
    }

    class SeqReportResponse
    {
        public List<string> Columns { get; set; }
        public List<List<string>> Rows { get; set; }
        public SeqReportStatistics Statistics { get; set; }
    }

    public class SeqRepoOptions
    {
        public string ServerUrl { get; set; }
    }

    public class SeqRepo
    {
        private SeqRepoOptions _options;
        private HttpClient _httpClient;
        private string _serverScheme;
        private string _serverHost;

        public SeqRepo(SeqRepoOptions options, HttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;

            var serverUrl = new Uri(_options.ServerUrl);
            _serverScheme = serverUrl.Scheme;
            _serverHost = serverUrl.Host;
        }

        private Uri GetReportUri(string query)
        {
            var builder = new UriBuilder();
            builder.Scheme = _serverScheme;
            builder.Host = _serverHost;
            builder.Path = "api/data";
            builder.Query = "q=" + Uri.EscapeDataString(query);
            return builder.Uri;
        }

        private async Task<List<string>> GetServiceList(string query)
        {
            var results = new List<string>();
            var requestUri = GetReportUri(query);
            var response = await _httpClient.PostAsJsonAsync(requestUri, new SeqReportRequest());
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var reportResponse = JsonConvert.DeserializeObject<SeqReportResponse>(responseContent);
            foreach (var row in reportResponse.Rows)
            {
                results.Add(row[0]);
            }
            return results;
        }

        public async Task<List<string>> GetAllServices()
        {
            return await GetServiceList("select count(*) from stream where @Timestamp > Now() - 1d group by CONCAT(CONCAT(Service, ':'), Environment)");
        }

        public async Task<List<string>> GetServicesWithCurrentFailures()
        {
            return await GetServiceList("select count(*) from stream where @Timestamp > Now() - 5m and (@Level = 'Error' or @Level = 'Critical') group by CONCAT(CONCAT(Service, ':'), Environment)");
        }

        public async Task<List<string>> GetServicesWithCurrentWarnings()
        {
            return await GetServiceList("select count(*) from stream where @Timestamp > Now() - 10m and @Level = 'Warning' group by CONCAT(CONCAT(Service, ':'), Environment)");
        }

        public async Task<List<string>> GetServicesWithPreviousFailures()
        {
            return await GetServiceList("select count(*) from stream where @Timestamp < Now() - 5m and @Timestamp > Now() - 10m and (@Level = 'Error' or @Level = 'Critical') group by CONCAT(CONCAT(Service, ':'), Environment)");
        }

        public async Task<List<string>> GetServicesWithCurrentNonFailureEvents()
        {
            return await GetServiceList("select count(*) from stream where @Timestamp > Now() - 5m and @Level = 'Information' group by CONCAT(CONCAT(Service, ':'), Environment)");
        }
    }
}
