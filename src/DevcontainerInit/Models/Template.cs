namespace DevcontainerInit.Models;

public class Template
{
    public string Name { get; set; } = string.Empty;
    public bool HasDockerfile { get; set; }
    public bool HasReadme { get; set; }
    public string? Description { get; set; }
}
