using OpenKNX.Toolbox.Lib.Data;
using System.Text.RegularExpressions;

namespace OpenKNX.Toolbox.Lib.Helper;

public static class GitHubAccess
{
    private const int OPEN_KNX_SEMANTIC_VERSION = 0;
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
        
        //json_response = json_response.Replace("v0.1.0-ALPHA", "0.1.0-ALPHA");
        Models.Github.OpenKnxContent? content = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.Github.OpenKnxContent>(json_response);
        if(content == null)
            throw new Exception("Error while deserializing JSON response.");

        if(content.GetSemanticVersion().Major < OPEN_KNX_SEMANTIC_VERSION)
            throw new Exception($"OpenKNX Toolbox version is not compatible with the current OpenKNX repository version. ({OPEN_KNX_SEMANTIC_VERSION}/{content.GetSemanticVersion().Major})");

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
                if(tag.ToLower().StartsWith("v"))
                    tag = tag.Substring(1);
                System.Management.Automation.SemanticVersion? version;
                
                try{
                    version = new System.Management.Automation.SemanticVersion(tag);
                }
                catch
                {
                    // Handle the case where the tag is not a valid semantic version
                    // For example, if the tag is "v1.0.0-alpha", you might want to skip it
                    continue;
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
                        Major = version.Major,
                        Minor = version.Minor,
                        Build = version.Patch,
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