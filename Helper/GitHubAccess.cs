using OpenKNX.Toolbox.Lib.Data;
using System.Text.RegularExpressions;

namespace OpenKNX.Toolbox.Lib.Helper;

public static class GitHubAccess
{
    private const string OPEN_KNX_ORG = "OpenKNX";
    private const string OPEN_KNX_REPO_DEFAULT_START = "OAM-";
    private const string OPEN_KNX_DATA_FILE_NAME = "OpenKNX.Toolbox.DataCache.json";

    private static List<string> repositoryWhitelist = new List<string>()
    {
        "SOM-UP",
        "GW-REG1-Dali",
        "SEN-UP1-8xTH",
        "BEM-GardenControl"
    };

    /// <summary>
    /// Returns OpenKNX Repository data from GitHub.
    /// </summary>
    /// <returns>Returns a List of "Repository" object in case of success.</returns>
    public static async Task<List<Repository>> GetOpenKnxRepositoriesAsync(bool includePreRelease = false)
    {
        List<Repository> repos = new List<Repository>();
        Octokit.GitHubClient client = new (new Octokit.ProductHeaderValue(OPEN_KNX_ORG));
        
        string? token = Environment.GetEnvironmentVariable("GITHUB_TOKEN", EnvironmentVariableTarget.User);
        if(!string.IsNullOrEmpty(token))
        {
            System.Console.WriteLine("Using Token: " + token);
            Octokit.Credentials tokenAuth = new (token); // NOTE: not real token
            client.Credentials = tokenAuth;
        }

        var repositories = await client.Repository.GetAllForOrg(OPEN_KNX_ORG);
        foreach (var repository in repositories)
        {
            if (!repository.Name.StartsWith(OPEN_KNX_REPO_DEFAULT_START) &&
                !repositoryWhitelist.Contains(repository.Name))
                continue;

            Repository repo = new () {
                Id = repository.Id,
                Name = repository.Name
            };

            var releases = await client.Repository.Release.GetAll(repository.Id);
            foreach (var release in releases)
            {
                if (string.IsNullOrEmpty(release.Name) || (!includePreRelease && release.Prerelease))
                    continue;

                string tag = release.TagName;
                Regex regex = new Regex("([0-9]+).([0-9]+).([0-9]+)");
                Match m = regex.Match(tag);
                int major = 0, minor = 0, build = 0;
                if(m.Success) 
                {
                    major = int.Parse(m.Groups[1].Value);
                    minor = int.Parse(m.Groups[2].Value);
                    build = int.Parse(m.Groups[3].Value);
                } else 
                {
                    regex = new Regex("([0-9]+).([0-9]+)");
                    m = regex.Match(tag);
                    if(m.Success)
                    {
                        major = int.Parse(m.Groups[1].Value);
                        minor = int.Parse(m.Groups[2].Value);
                    } else {

                    }
                }

                foreach (var asset in release.Assets)
                {
                    if (!asset.Name.ToLower().EndsWith(".zip"))
                        continue;
                    
                    repo.Releases.Add(new() {
                        Id = asset.Id,
                        Name = asset.Name,
                        Url = asset.BrowserDownloadUrl,
                        IsPrerelease = release.Prerelease,
                        Major = major,
                        Minor = minor,
                        Build = build,
                        Published = release.PublishedAt
                    });
                }

                repo.Releases.Sort((a, b) => a.CompareTo(b));
            }

            if(repo.Releases.Count > 0)
                repos.Add(repo);
        }

        repos.Sort((a, b) => a.Name.CompareTo(b.Name));

        return repos;
    }

    public static async Task DownloadRepo(string url, string targetPath)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                var fileStream = new FileStream(targetPath, System.IO.FileMode.Create);
                await contentStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
                fileStream.Close();
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
    }
}