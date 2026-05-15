using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using DevcontainerInit.Models;

namespace DevcontainerInit.Services;

public class GitHubService
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _templatesPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GitHubService(AppConfig config)
    {
        _owner = config.GitHubOwner;
        _repo = config.GitHubRepo;
        _templatesPath = config.TemplatesPath;

        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("devcontainer-init", "1.0"));
    }

    public async Task<List<Template>> GetTemplatesAsync()
    {
        var url = $"https://api.github.com/repos/{_owner}/{_repo}/git/trees/HEAD?recursive=1";
        var json = await _http.GetStringAsync(url);
        var response = JsonSerializer.Deserialize<GitTreeResponse>(json, JsonOptions)!;

        var paths = response.Tree.Select(t => t.Path).ToHashSet();
        var prefix = _templatesPath + "/";

        return response.Tree
            .Where(t => t.Type == "tree" &&
                        t.Path.StartsWith(prefix) &&
                        !t.Path[prefix.Length..].Contains('/'))
            .Select(t =>
            {
                var name = t.Path[prefix.Length..];
                return new Template
                {
                    Name = name,
                    HasDockerfile = paths.Contains($"{prefix}{name}/Dockerfile"),
                    HasReadme = paths.Contains($"{prefix}{name}/README.md"),
                };
            })
            .OrderBy(t => t.Name)
            .ToList();
    }

    public async Task<string> GetFileContentAsync(string path)
    {
        var url = $"https://raw.githubusercontent.com/{_owner}/{_repo}/HEAD/{path}";
        return await _http.GetStringAsync(url);
    }

    public string GetDevcontainerJsonPath(string templateName) =>
        $"{_templatesPath}/{templateName}/devcontainer.json";

    public string GetDockerfilePath(string templateName) =>
        $"{_templatesPath}/{templateName}/Dockerfile";

    public string GetReadmePath(string templateName) =>
        $"{_templatesPath}/{templateName}/README.md";

    private record GitTreeResponse(
        [property: JsonPropertyName("tree")] List<GitTreeItem> Tree);

    private record GitTreeItem(
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("type")] string Type);
}
