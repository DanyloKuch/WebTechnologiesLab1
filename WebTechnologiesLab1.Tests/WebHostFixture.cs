using System.Diagnostics;

namespace WebTechnologiesLab1.Tests;

// -----------------------------------------------------------------------------
// Стартує веб-додаток WebTechnologiesLab1 один раз для всього класу тестів.
// Якщо сайт уже запущений (напр., через дебагер VS) — переюзає його,
// нового процесу не створює і нічого не вбиває.
// -----------------------------------------------------------------------------
public sealed class WebHostFixture : IDisposable
{
    private const string BaseUrl = "https://localhost:7062";
    private const int StartupTimeoutSeconds = 180;

    private readonly Process? _process;

    public WebHostFixture()
    {
        if (IsSiteUp())
        {
            return;
        }

        var projectDir = FindWebProjectDirectory()
            ?? throw new InvalidOperationException(
                "Не вдалося знайти WebTechnologiesLab1.csproj — перевір структуру solution.");

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --launch-profile https",
                WorkingDirectory = projectDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            EnableRaisingEvents = true,
        };

        _process.OutputDataReceived += (_, _) => { };
        _process.ErrorDataReceived += (_, _) => { };
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        var deadline = DateTime.UtcNow.AddSeconds(StartupTimeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException(
                    $"Веб-додаток зупинився передчасно (exit code {_process.ExitCode}).");
            }

            if (IsSiteUp())
            {
                return;
            }

            Thread.Sleep(1000);
        }

        TryKill();
        throw new InvalidOperationException(
            $"Веб-додаток не став доступним на {BaseUrl} за {StartupTimeoutSeconds} секунд.");
    }

    public void Dispose()
    {
        TryKill();
    }

    private void TryKill()
    {
        if (_process is null || _process.HasExited)
        {
            return;
        }

        try
        {
            _process.Kill(entireProcessTree: true);
            _process.WaitForExit(10_000);
        }
        catch
        {
            // ігноруємо — процес уже міг вмерти
        }
        finally
        {
            _process.Dispose();
        }
    }

    private static bool IsSiteUp()
    {
        try
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
            using var response = client.GetAsync(BaseUrl).GetAwaiter().GetResult();
            return (int)response.StatusCode is >= 200 and < 600;
        }
        catch
        {
            return false;
        }
    }

    private static string? FindWebProjectDirectory()
    {
        // Піднімаємось від bin/Debug/net9.0 шукаючи .sln; звідти переходимо в підпапку проєкту.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("WebTechnologiesLab1.sln").Length > 0)
            {
                var projectDir = Path.Combine(dir.FullName, "WebTechnologiesLab1");
                if (File.Exists(Path.Combine(projectDir, "WebTechnologiesLab1.csproj")))
                {
                    return projectDir;
                }
            }
            dir = dir.Parent;
        }
        return null;
    }
}
