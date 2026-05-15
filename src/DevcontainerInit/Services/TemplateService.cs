using DevcontainerInit.Models;

namespace DevcontainerInit.Services;

public class TemplateService
{
    private readonly GitHubService _github;
    private readonly DockerService _docker;

    public TemplateService(GitHubService github, DockerService docker)
    {
        _github = github;
        _docker = docker;
    }

    public async Task<List<Template>> GetTemplatesAsync()
    {
        var templates = await _github.GetTemplatesAsync();

        foreach (var template in templates)
        {
            template.HasDockerfile = await _github.FileExistsAsync(
                _github.GetDockerfilePath(template.Name));
        }

        return templates;
    }

    public async Task<string> DownloadDevcontainerJsonAsync(string templateName)
    {
        var path = _github.GetDevcontainerJsonPath(templateName);
        return await _github.GetFileContentAsync(path);
    }

    public async Task<string?> DownloadDockerfileAsync(string templateName)
    {
        var path = _github.GetDockerfilePath(templateName);
        if (!await _github.FileExistsAsync(path))
            return null;

        return await _github.GetFileContentAsync(path);
    }

    public bool ImageExists(string imageName) => _docker.ImageExists(imageName);

    public bool BuildImage(string imageName, string dockerfilePath) =>
        _docker.BuildImage(imageName, dockerfilePath);
}
