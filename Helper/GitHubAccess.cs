using OpenKNX.Toolbox.Lib.Data;
using OpenKNX.Toolbox.Lib.Models;
using System.Diagnostics;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace OpenKNX.Toolbox.Lib.Helper;

public static class GitHubAccess
{
    private const int OPEN_KNX_SEMANTIC_VERSION = 0;


    private static List<RepositoryMapping> RepoMappings = new List<RepositoryMapping>()
    {
        new RepositoryMapping("$A030", "OAM-LogicModule", "release"),
        new RepositoryMapping("$A031", "OAM-LogicModule", "dev"),
        // ing-dom
        new RepositoryMapping("$A102", "SEN-UP1-8xTH", "release"),
        new RepositoryMapping("$A103", "SEN-UP1-8xTH", "beta"),
        new RepositoryMapping("$A11F", "OAM-IP-Router", "release"),
        new RepositoryMapping("$A11E", "OAM-IP-Router", "beta"),
        // smart-mf
        new RepositoryMapping("$A228", "SOM-UP", "release"),
        new RepositoryMapping("$A229", "SOM-UP", "dev"),
        // thewhobox
        new RepositoryMapping("$A400", "OAM-InfraredGateway", "release"),
        new RepositoryMapping("$A401", "GW-REG1-Dali", "release"),
        new RepositoryMapping("$A402", "Omote", "release"),
        // traxanos
        new RepositoryMapping("$A302", "VirtualButtonModule", "release"),
        new RepositoryMapping("$A303", "VirtualButtonModule", "beta"),
        // mgeramb
        new RepositoryMapping("$AE29", "OAM-SmartHomeBridge", "dev"),
        new RepositoryMapping("$AE2A", "OAM-SmartHomeBridge", "release"),
        new RepositoryMapping("$AE2B", "OAM-Sonos", "dev"),
        new RepositoryMapping("$AE2C", "OAM-Sonos", "release"),
        new RepositoryMapping("$AE2D", "OAM-InternetServices", "dev"),
        new RepositoryMapping("$AE2E", "OAM-InternetServices", "release"),
        new RepositoryMapping("$AE2F", "OAM-InternetServices", "dev"),
        new RepositoryMapping("$AE30", "OAM-InternetServices", "release"),
        new RepositoryMapping("$AE31", "OAM-ShutterControl", "dev"),
        new RepositoryMapping("$AE32", "OAM-InternetServices", "release"),
    };

    /// <summary>
    /// Returns OpenKNX Repository data from GitHub.
    /// </summary>
    /// <returns>Returns a List of "Repository" object in case of success.</returns>
    public static async Task<List<Models.Application>> GetOpenKnxApplicationsAsync(IProgress<KeyValuePair<long, long>>? progress = null)
    {
        List<Application> apps = new();

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

            List<string> labels = GetLabels(repo.Value);
            if(labels.Count == 0)
                labels.Add("release");


            foreach (string label in labels)
            {
                RepositoryMapping? map = RepoMappings.FirstOrDefault(m => m.Name == repo.Key && m.Label == label);
                if (map == null)
                {
                    Debug.WriteLine($"No AppId found for {repo.Key} with label {label}");
                    continue;
                }

                Application app = new();
                app.Name = map.Name;
                app.AppId = map.AppId;
                app.Label = map.Label;

                GetAppReleases(app, repo.Value);

                if (app.Releases.Count > 0)
                    apps.Add(app);
            }
        }

        progress?.Report(new KeyValuePair<long, long>(index, content.Repositories.Count));
        

        return apps;
    }

    private static List<string> GetLabels(Models.Github.Repository repo)
    {
        List<string> labels = new();
        foreach(var release in repo.Releases)
        {
            string tag = release.Tag;
            if (tag.StartsWith("V") || tag.StartsWith("v"))
                tag = tag.Substring(1);

            SemanticVersion? version = GetSemanticVersion(tag);
            if (version == null)
                version = GetSemanticVersionWithRegex(release.Name);
            if(version == null)
            {
                Debug.WriteLine($"Invalid semantic version: {release.Name}/{tag}");
                continue;
            }

            string preRelease = version.PreReleaseLabel?.ToLower() ?? "release";
            if (!string.IsNullOrEmpty(preRelease) && !labels.Contains(preRelease))
                labels.Add(preRelease);
        }
        return labels;
    }

    private static SemanticVersion? GetSemanticVersion(string tag)
    {
        if (tag.StartsWith("V") || tag.StartsWith("v"))
            tag = tag.Substring(1);
        SemanticVersion version;
        try
        {
            version = new SemanticVersion(tag);
        }
        catch
        {
            return GetSemanticVersionWithRegex(tag);
        }
        return version;
    }

    private static SemanticVersion? GetSemanticVersionWithRegex(string tag)
    {
        Regex regex = new Regex(@"(\d+)\.(\d+)\.(\d+)(-(.+))?");
        Match m = regex.Match(tag);
        if(m.Success)
        {

        } else
        {
            regex = new Regex(@"(\d+)\.(\d+)(-(.+))?");
            m = regex.Match(tag);
            return new SemanticVersion(m.Groups[0].ToString());
        }
        return null;
    }

    private static void GetAppReleases(Application app, Models.Github.Repository repo)
    {
        foreach(var release in repo.Releases)
        {
            string tag = release.Tag;
            if (tag.StartsWith("V") || tag.StartsWith("v"))
                tag = tag.Substring(1);

            SemanticVersion? version = GetSemanticVersion(tag);
            if (version == null)
                version = GetSemanticVersionWithRegex(release.Name);
            if (version == null)
            {
                Debug.WriteLine($"Invalid semantic version: {release.Name}/{tag}");
                continue;
            }

            string label = version.PreReleaseLabel?.ToLower() ?? "release";
            if (label != app.Label)
                continue;

            foreach(var asset in release.Assets)
            {
                if (!asset.Name.EndsWith(".zip"))
                    continue;

                AppRelease appRelease = new(asset.Name, asset.Url, version);
                appRelease.PublishedAt = asset.Updated;
                appRelease.IsPrerelease = release.IsPrerelease;

                app.Releases.Add(appRelease);
            }
        }
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