using DevcontainerInit.Models;
using Octokit;

namespace DevcontainerInit.Services;

public class GitHubService
{
    private readonly GitHubClient _client;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _templatesPath;

    public GitHubService(AppConfig config)
    {
        _client = new GitHubClient(new ProductHeaderValue("devcontainer-init"));
        _owner = config.GitHubOwner;
        _repo = config.GitHubRepo;
        _templatesPath = config.TemplatesPath;
    }

    public async Task<List<Template>> GetTemplatesAsync()
    {
        var tree = await _client.Git.Tree.GetRecursive(_owner, _repo, "HEAD");
        var paths = tree.Tree.Select(t => t.Path).ToHashSet();
        var prefix = _templatesPath + "/";

        return tree.Tree
            .Where(t => t.Type == TreeType.Tree &&
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
        var contents = await _client.Repository.Content.GetAllContents(_owner, _repo, path);
        return contents[0].Content;
    }

    public string GetDevcontainerJsonPath(string templateName) =>
        $"{_templatesPath}/{templateName}/devcontainer.json";

    public string GetDockerfilePath(string templateName) =>
        $"{_templatesPath}/{templateName}/Dockerfile";

    public string GetReadmePath(string templateName) =>
        $"{_templatesPath}/{templateName}/README.md";
}
