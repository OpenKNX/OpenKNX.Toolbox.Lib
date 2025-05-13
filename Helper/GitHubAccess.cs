using OpenKNX.Toolbox.Lib.Data;
using System.Text.RegularExpressions;

namespace OpenKNX.Toolbox.Lib.Helper;

public static class GitHubAccess
{
    private const string OPEN_KNX_ORG = "OpenKNX";
    private const string OPEN_KNX_REPO_DEFAULT_START = "OAM-";
    private const string OPEN_KNX_DATA_FILE_NAME = "OpenKNX.Toolbox.DataCache.json";
    private static List<string> repositoryWhitelist = new List<string>();

    /// <summary>
    /// Returns OpenKNX Repository data from GitHub.
    /// </summary>
    /// <returns>Returns a List of "Repository" object in case of success.</returns>
    public static async Task<List<Repository>> GetOpenKnxRepositoriesAsync(IProgress<KeyValuePair<long, long>>? progress = null)
    {
        List<Repository> repos = new List<Repository>();

        HttpClient client = new HttpClient();
        string json_response = await client.GetStringAsync("https://openknx.github.io/releases.json");
        
        Models.Github.OpenKnxContent? content = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Github.OpenKnxContent>(json_response);
        if(content == null)
            throw new Exception("Error while deserializing JSON response.");

        int index = 0;
        foreach(var repo in content.Repositories)
        {
            progress?.Report(new KeyValuePair<long, long>(index, content.Repositories.Count));
            index++;

            Repository repository = new Repository(repo.Key, repo.Value.Url);
            if(repo.Value.IsArchived)
                repository.Name += " (archived)";

            foreach(var release in repo.Value.Releases)
            {
                string tag = release.Tag;
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
                        Console.WriteLine("Keine Version gefunden");
                    }
                }

                foreach (var asset in release.Assets)
                {
                    if (!asset.Name.ToLower().EndsWith(".zip"))
                        continue;

                    Release rel = new() {
                        Name = asset.Name,
                        Url = asset.Url,
                        UrlRelease = release.Url,
                        IsPrerelease = release.IsPrerelease,
                        Major = major,
                        Minor = minor,
                        Build = build,
                        Published = release.PublishedAt
                    };
                    
                    repository.ReleasesAll.Add(rel);
                }

                repository.ReleasesAll.Sort((a, b) => a.CompareTo(b));
            }

            if(repository.ReleasesAll.Count > 0)
                repos.Add(repository);
        }

        progress?.Report(new KeyValuePair<long, long>(index, content.Repositories.Count));

        // var repositories = await client.Repository.GetAllForOrg(OPEN_KNX_ORG);
        // int index = 0;
        // foreach (var repository in repositories)
        // {
        //     progress?.Report(new KeyValuePair<long, long>(index, repositories.Count));
        //     index++;

        //     if (!repository.Name.StartsWith(OPEN_KNX_REPO_DEFAULT_START) &&
        //         !repositoryWhitelist.Contains(repository.Name))
        //         continue;

        //     Repository repo = new () {
        //         Id = repository.Id,
        //         Name = repository.Name
        //     };

        //     var releases = await client.Repository.Release.GetAll(repository.Id);
        //     foreach (var release in releases)
        //     {
        //         string tag = release.TagName;
        //         Regex regex = new Regex("([0-9]+).([0-9]+).([0-9]+)");
        //         Match m = regex.Match(tag);
        //         int major = 0, minor = 0, build = 0;
        //         if(m.Success) 
        //         {
        //             major = int.Parse(m.Groups[1].Value);
        //             minor = int.Parse(m.Groups[2].Value);
        //             build = int.Parse(m.Groups[3].Value);
        //         } else 
        //         {
        //             regex = new Regex("([0-9]+).([0-9]+)");
        //             m = regex.Match(tag);
        //             if(m.Success)
        //             {
        //                 major = int.Parse(m.Groups[1].Value);
        //                 minor = int.Parse(m.Groups[2].Value);
        //             } else {
        //                 Console.WriteLine("Keine Version gefunden");
        //             }
        //         }

        //         foreach (var asset in release.Assets)
        //         {
        //             if (!asset.Name.ToLower().EndsWith(".zip"))
        //                 continue;

        //             Release rel = new() {
        //                 Id = asset.Id,
        //                 Name = asset.Name,
        //                 Url = asset.BrowserDownloadUrl,
        //                 UrlRelease = release.HtmlUrl,
        //                 IsPrerelease = release.Prerelease,
        //                 Major = major,
        //                 Minor = minor,
        //                 Build = build,
        //                 Published = release.PublishedAt
        //             };
                    
        //             repo.ReleasesAll.Add(rel);
        //         }

        //         repo.ReleasesAll.Sort((a, b) => a.CompareTo(b));
        //     }

        //     if(repo.ReleasesAll.Count > 0)
        //         repos.Add(repo);
        // }
        
        //progress?.Report(new KeyValuePair<long, long>(index, repositories.Count));

        repos.Sort((a, b) => a.Name.CompareTo(b.Name));

        return repos;
    }

    public static async Task DownloadRepo(string url, string targetPath, IProgress<KeyValuePair<long, long>>? progress = null)
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                var fileStream = new FileStream(targetPath, System.IO.FileMode.Create);
                //await contentStream.CopyToAsync(fileStream);
                byte[] buffer = new byte[1024];
                int readedBytes = 0;
                int wroteBytes = 0;
                while(true) {
                    readedBytes = contentStream.Read(buffer, 0, 1024);
                    fileStream.Write(buffer, 0, readedBytes);
                    wroteBytes += readedBytes;
                    progress?.Report(new KeyValuePair<long, long>(wroteBytes, contentStream.Length));
                    if(readedBytes < 1024)
                        break;
                }
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