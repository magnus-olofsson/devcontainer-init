using System.Diagnostics;

namespace DevcontainerInit.Services;

public class DockerService
{
    public bool ImageExists(string imageName)
    {
        var result = RunDockerCommand($"images -q {imageName}");
        return !string.IsNullOrWhiteSpace(result.Output);
    }

    public bool BuildImage(string imageName, string dockerfilePath)
    {
        var buildDir = Path.GetDirectoryName(dockerfilePath)!;
        var result = RunDockerCommand($"build -t {imageName} {buildDir}", streamOutput: true);
        return result.ExitCode == 0;
    }

    private (string Output, int ExitCode) RunDockerCommand(string arguments, bool streamOutput = false)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = arguments,
                RedirectStandardOutput = !streamOutput,
                RedirectStandardError = !streamOutput,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var output = streamOutput ? string.Empty : process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return (output, process.ExitCode);
    }
}
