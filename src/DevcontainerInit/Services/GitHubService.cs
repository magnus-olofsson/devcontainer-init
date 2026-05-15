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
        var contents = await _client.Repository.Content.GetAllContents(_owner, _repo, _templatesPath);

        return contents
            .Where(c => c.Type == ContentType.Dir)
            .Select(c => new Template { Name = c.Name })
            .OrderBy(t => t.Name)
            .ToList();
    }

    public async Task<bool> FileExistsAsync(string path)
    {
        try
        {
            await _client.Repository.Content.GetAllContents(_owner, _repo, path);
            return true;
        }
        catch (NotFoundException)
        {
            return false;
        }
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
}
