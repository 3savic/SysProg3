using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SysProg3
{
    public class GitHubSearchService
    {
        private readonly GitHubClient _client;
        private readonly LRUCache<string, List<RepositoryInfo>> _cache;

        public GitHubSearchService(int cacheSize)
        {
            _client = new GitHubClient();
            _cache = new LRUCache<string, List<RepositoryInfo>>(cacheSize);
        }

        public IObservable<List<RepositoryInfo>> GetRepositories(string language)
        {
            return Observable.Create<List<RepositoryInfo>>(async observer =>
            {
                if (_cache.TryRead(language, out var cachedRepos))
                {
                    observer.OnNext(cachedRepos);
                    observer.OnCompleted();
                }
                else
                {
                    try
                    {
                        var repos = await _client.GetRepositoriesAsync(language);
                        var repoInfos = await Task.WhenAll(repos.Select(async repo =>
                        {
                            var parts = repo.FullName.Split('/');
                            var contributors = await _client.GetContributorsAsync(parts[0], parts[1]);
                            return new RepositoryInfo
                            {
                                FullName = repo.FullName,
                                Description = repo.Description,
                                HtmlUrl = repo.HtmlUrl,
                                Contributors = contributors
                            };
                        }).ToList());

                        _cache.Write(language, repoInfos.ToList());
                        observer.OnNext(repoInfos.ToList());
                        observer.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        observer.OnError(ex);
                    }
                }
            })
            .SubscribeOn(NewThreadScheduler.Default)
            .ObserveOn(TaskPoolScheduler.Default);
        }
    }
    public class RepositoryInfo
    {
        public string FullName { get; set; }
        public string Description { get; set; }
        public string HtmlUrl { get; set; }
        public List<Contributor> Contributors { get; set; }
    }
}
