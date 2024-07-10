using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

namespace SysProg3
{
    public class GitHubClient
    {
        private readonly RestClient _client;

        public GitHubClient()
        {
            _client = new RestClient("https://api.github.com");
        }

        public async Task<List<Repository>> GetRepositoriesAsync(string language)
        {
            var request = new RestRequest("search/repositories", Method.Get);
            request.AddQueryParameter("q", $"language:{language}");

            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new Exception(response.ErrorMessage);
            }

            var content = JsonConvert.DeserializeObject<GitHubResponse>(response.Content);
            return content.Items;
        }

        public async Task<List<Contributor>> GetContributorsAsync(string owner, string repo)
        {
            var request = new RestRequest($"repos/{owner}/{repo}/contributors", Method.Get);

            var response = await _client.ExecuteAsync(request);
            if (!response.IsSuccessful)
            {
                throw new Exception(response.ErrorMessage);
            }

            var content = JsonConvert.DeserializeObject<List<Contributor>>(response.Content);
            return content;
        }
    }

    public class GitHubResponse
    {
        [JsonProperty("items")]
        public List<Repository> Items { get; set; }
    }

    public class Repository
    {
        [JsonProperty("full_name")]
        public string FullName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("html_url")]
        public string HtmlUrl { get; set; }
    }
    public class Contributor
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("contributions")]
        public int Contributions { get; set; }
    }
}
