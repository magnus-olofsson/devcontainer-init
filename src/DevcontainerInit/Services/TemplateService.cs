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

        foreach (var template in templates.Where(t => t.HasReadme))
        {
            var readme = await _github.GetFileContentAsync(_github.GetReadmePath(template.Name));
            template.Description = readme
                .Split('\n')
                .Select(l => l.Trim())
                .FirstOrDefault(l => l.Length > 0 && !l.StartsWith('#'));
        }

        return templates;
    }

    public async Task<string> DownloadDevcontainerJsonAsync(string templateName)
    {
        return await _github.GetFileContentAsync(_github.GetDevcontainerJsonPath(templateName));
    }

    public async Task<string> DownloadDockerfileAsync(string templateName)
    {
        return await _github.GetFileContentAsync(_github.GetDockerfilePath(templateName));
    }

    public bool ImageExists(string imageName) => _docker.ImageExists(imageName);

    public bool BuildImage(string imageName, string dockerfilePath) =>
        _docker.BuildImage(imageName, dockerfilePath);
}
