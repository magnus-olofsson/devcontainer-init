namespace DevcontainerInit.Models;

public class AppConfig
{
    public string GitHubOwner { get; set; } = string.Empty;
    public string GitHubRepo { get; set; } = string.Empty;
    public string TemplatesPath { get; set; } = "templates";
}
