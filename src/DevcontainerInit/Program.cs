using System.Text.Json;
using DevcontainerInit.Models;
using DevcontainerInit.Services;
using Spectre.Console;

var configService = new ConfigService();

// --- First run: create global config if missing ---
if (!configService.GlobalConfigExists())
{
    AnsiConsole.MarkupLine("[yellow]Ingen global config hittades.[/]");
    AnsiConsole.MarkupLine($"Skapar config på: [cyan]{configService.GetGlobalConfigPath()}[/]\n");

    var owner = AnsiConsole.Ask<string>("GitHub-användarnamn eller organisation:");
    var repo = AnsiConsole.Ask<string>("GitHub-repo namn:");
    var templatesPath = AnsiConsole.Ask<string>("Sökväg till templates i repot:", "templates");

    var newConfig = new AppConfig
    {
        GitHubOwner = owner,
        GitHubRepo = repo,
        TemplatesPath = templatesPath
    };

    configService.SaveGlobalConfig(newConfig);
    AnsiConsole.MarkupLine("\n[green]Config sparad![/]\n");
}

// --- Load config (global + eventuell lokal override) ---
var config = configService.Load();

if (string.IsNullOrWhiteSpace(config.GitHubOwner) || string.IsNullOrWhiteSpace(config.GitHubRepo))
{
    AnsiConsole.MarkupLine("[red]Config saknar GitHubOwner eller GitHubRepo. Kontrollera din config.[/]");
    AnsiConsole.MarkupLine($"Global config: [cyan]{configService.GetGlobalConfigPath()}[/]");
    AnsiConsole.MarkupLine($"Lokal config:  [cyan]{configService.GetLocalConfigPath()}[/]");
    return;
}

AnsiConsole.MarkupLine($"Hämtar templates från [cyan]{config.GitHubOwner}/{config.GitHubRepo}[/]...\n");

// --- Hämta templates ---
var github = new GitHubService(config);
var docker = new DockerService();
var templateService = new TemplateService(github, docker);

List<Template> templates = [];

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Hämtar templates...", async _ =>
    {
        templates = await templateService.GetTemplatesAsync();
    });

if (templates.Count == 0)
{
    AnsiConsole.MarkupLine("[red]Inga templates hittades i repot.[/]");
    return;
}

// --- Visa numrerad lista ---
AnsiConsole.MarkupLine("[bold]Tillgängliga templates:[/]\n");
for (int i = 0; i < templates.Count; i++)
{
    var dockerTag = templates[i].HasDockerfile ? " [grey](inkl. Dockerfile)[/]" : "";
    AnsiConsole.MarkupLine($"  [cyan]{i + 1}[/]. {templates[i].Name}{dockerTag}");
}

AnsiConsole.WriteLine();
var choice = AnsiConsole.Ask<int>("Välj template (nummer):");

if (choice < 1 || choice > templates.Count)
{
    AnsiConsole.MarkupLine("[red]Ogiltigt val.[/]");
    return;
}

var selected = templates[choice - 1];
AnsiConsole.MarkupLine($"\nValde: [green]{selected.Name}[/]\n");

// --- Skapa .devcontainer-mapp ---
var devcontainerDir = Path.Combine(Directory.GetCurrentDirectory(), ".devcontainer");
Directory.CreateDirectory(devcontainerDir);

// --- Ladda ner devcontainer.json ---
string devcontainerJson = string.Empty;

await AnsiConsole.Status()
    .Spinner(Spinner.Known.Dots)
    .StartAsync("Laddar ner devcontainer.json...", async _ =>
    {
        devcontainerJson = await templateService.DownloadDevcontainerJsonAsync(selected.Name);
    });

var devcontainerPath = Path.Combine(devcontainerDir, "devcontainer.json");
File.WriteAllText(devcontainerPath, devcontainerJson);
AnsiConsole.MarkupLine($"[green]✓[/] devcontainer.json sparad till [cyan]{devcontainerPath}[/]");

// --- Hantera Dockerfile och Docker image ---
if (selected.HasDockerfile)
{
    // Försök läsa ut image-namnet ur devcontainer.json
    string? imageName = null;
    try
    {
        var doc = JsonDocument.Parse(devcontainerJson);
        if (doc.RootElement.TryGetProperty("image", out var imageEl))
            imageName = imageEl.GetString();
    }
    catch { /* ignore parse errors */ }

    if (imageName is not null && templateService.ImageExists(imageName))
    {
        AnsiConsole.MarkupLine($"[green]✓[/] Docker image [cyan]{imageName}[/] finns redan lokalt.");
    }
    else
    {
        if (imageName is not null)
            AnsiConsole.MarkupLine($"[yellow]Docker image [cyan]{imageName}[/] hittades inte lokalt.[/]");
        else
            AnsiConsole.MarkupLine("[yellow]Kunde inte avgöra image-namn från devcontainer.json.[/]");

        var buildImage = AnsiConsole.Confirm("Vill du ladda ner Dockerfile och bygga imagen?");

        if (buildImage)
        {
            string? dockerfileContent = null;

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Laddar ner Dockerfile...", async _ =>
                {
                    dockerfileContent = await templateService.DownloadDockerfileAsync(selected.Name);
                });

            if (dockerfileContent is null)
            {
                AnsiConsole.MarkupLine("[red]Kunde inte hämta Dockerfile.[/]");
                return;
            }

            var dockerfilePath = Path.Combine(devcontainerDir, "Dockerfile");
            File.WriteAllText(dockerfilePath, dockerfileContent);
            AnsiConsole.MarkupLine($"[green]✓[/] Dockerfile sparad till [cyan]{dockerfilePath}[/]");

            var finalImageName = imageName ?? selected.Name;
            AnsiConsole.MarkupLine($"\nBygger Docker image [cyan]{finalImageName}[/]...\n");

            var success = templateService.BuildImage(finalImageName, dockerfilePath);

            if (success)
                AnsiConsole.MarkupLine($"\n[green]✓[/] Image [cyan]{finalImageName}[/] byggd!");
            else
                AnsiConsole.MarkupLine("\n[red]Något gick fel under docker build.[/]");
        }
    }
}

AnsiConsole.MarkupLine("\n[bold green]Klart! Öppna mappen i VS Code och kör 'Reopen in Container'.[/]");
