#!/usr/bin/env dotnet run
#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net10.0
#:property Nullable=enable
#:property ImplicitUsings=enable

// DXRP Server Launcher
// Requirements: .NET 10 SDK, Git
// Usage:        dotnet run dxrp-server.cs [--token YOUR_TOKEN]

using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

AppDomain.CurrentDomain.ProcessExit += (_, _) => Console.ResetColor();
await Launcher.Run(args);

static class Launcher
{
    const string ConfigFile = "dxrp-server-config.json";
    static string RepoDir = "dxrp";

    public static async Task Run(string[] args)
    {
        var config = LoadConfig();

        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == "--token") config.Token = args[i + 1];

        Banner();

        // [0] Preflight
        bool devMode = false;
        int total = 7;
        Header(0, total, "Preflight");

        if (!File.Exists("sbox-server.dll"))
        {
            if (Directory.Exists("game"))
            {
                devMode = true;
                total = 5;
                RepoDir = ".";
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠  DEV MODE");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("     sbox-server.dll not found but /game exists.");
                Console.WriteLine("     Addons will be patched in-place.");
                Console.WriteLine("     Repo will NOT be cloned. Server will NOT launch.");
                Console.ResetColor();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("  Confirm dev patch? [y/N]: ");
                Console.ResetColor();
                var answer = Console.ReadLine()?.Trim().ToLower() ?? "";
                if (answer != "y" && answer != "yes") Fatal("Aborted.");
                Ok("Dev mode confirmed.");
            }
            else
            {
                Fatal("sbox-server.dll not found in current directory.");
            }
        }
        else
        {
            Ok("Found sbox-server.dll");
        }

        // [1] Token
        Header(1, total, "Token");
        if (string.IsNullOrWhiteSpace(config.Token))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("  Enter server token: ");
            Console.ResetColor();
            config.Token = Console.ReadLine()?.Trim() ?? "";
        }
        if (string.IsNullOrWhiteSpace(config.Token))
            Fatal("No token provided.");
        SaveConfig(config);
        Ok($"Token: {Mask(config.Token)}");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("X-Server-Token", config.Token);
        var sbprojPath = Path.GetFullPath(Path.Combine(RepoDir, "game", "rp.sbproj"));

        while (true)
        {
            int step = 2;

            if (!devMode)
            {
                // [2] Repository
                Header(step++, total, "Repository");
                bool hasRepo = Directory.Exists(Path.Combine(RepoDir, ".git"));
                Info(hasRepo ? "Fetching latest..." : $"Cloning {config.RepoUrl} ({config.Branch})...");
                if (hasRepo)
                {
                    RunGit(["-C", RepoDir, "fetch", "--depth", "1", "origin", config.Branch]);
                    RunGit(["-C", RepoDir, "reset", "--hard", $"origin/{config.Branch}"]);
                }
                else
                {
                    RunGit(["clone", "--depth", "1", "--branch", config.Branch, config.RepoUrl, RepoDir]);
                }
                Ok("Repository ready.");
            }

            // Fetch addons
            Header(step++, total, "Fetching addons");
            List<AddonInfo> addons;
            try
            {
                var json = await http.GetStringAsync($"{config.ApiEndpoint}/v1/server/addons");
                addons = JsonSerializer.Deserialize(json, AppJsonContext.Default.ListAddonInfo)!;
            }
            catch (Exception ex) { Fatal($"API error: {ex.Message}"); return; }
            Ok($"{addons.Count} addon(s) found.");

            // Clear addon directories
            Header(step++, total, "Clearing addon directories");
            var codeBase = Path.Combine(RepoDir, "game", "Code", "Addons");
            var assetBase = Path.Combine(RepoDir, "game", "Assets", "addons");
            ClearDir(codeBase);
            ClearDir(assetBase);
            Ok("Cleared.");

            // Initial build
            Header(step++, total, "Initial build");
            if (config.VerifyAddons) { BuildOrFatal(); Ok("Build ready."); }
            else Ok("Skipped (verify disabled).");

            // Download & extract addons
            Header(step++, total, "Downloading addons");
            Console.WriteLine();
            for (int i = 0; i < addons.Count; i++)
            {
                var a = addons[i];

                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  [{i + 1}/{addons.Count}]  ");
                Console.ResetColor();
                Console.Write(a.AddonName);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  ({a.NetworkIdentifier}.{a.AddonIdentifier})");
                Console.WriteLine($"  r{a.RevisionNumber}");
                Console.ResetColor();

                if (!string.IsNullOrEmpty(a.CodePackageUrl))
                    await DownloadAndExtract(http, a.CodePackageUrl,
                        Path.Combine(codeBase, a.NetworkIdentifier, a.AddonIdentifier),
                        "code");

                if (!string.IsNullOrEmpty(a.AssetsPackageUrl))
                    await DownloadAndExtract(http, a.AssetsPackageUrl,
                        Path.Combine(assetBase, a.NetworkIdentifier, a.AddonIdentifier),
                        "assets");

                Console.WriteLine();
                if (config.VerifyAddons) BuildOrFatal();
            }
            Ok("All addons ready.");

            if (devMode)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("  ⚠  DEV  Addon patch complete. Server not launched.");
                Console.ResetColor();
                break;
            }

            // Launch
            Header(step, total, "Launching server");
            var launchArgs = $"+game \"{sbprojPath}\" +authorize {config.Token}";
            if (!string.IsNullOrWhiteSpace(config.Map))
                launchArgs += $" +map {config.Map}";
            if (!string.IsNullOrWhiteSpace(config.ExtraArgs))
                launchArgs += $" {config.ExtraArgs}";

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("  ▶  ");
            Console.ResetColor();
            Console.Write("sbox-server");
            if (!string.IsNullOrWhiteSpace(config.Map))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"  +map {config.Map}");
                Console.ResetColor();
            }
            Console.WriteLine("\n");

            var proc = Process.Start(new ProcessStartInfo("dotnet", $"\"sbox-server.dll\" {launchArgs}")
            {
                UseShellExecute = false,
            })!;
            await proc.WaitForExitAsync();

            const string O = "\x1b[38;2;255;140;0m";
            const string Rst = "\x1b[0m";
            string sep = "  " + new string('─', 44);
            Console.WriteLine();
            Console.WriteLine($"{O}{sep}{Rst}");
            for (int s = 5; s > 0; s--)
            {
                Console.Write($"\r{O}  ↺  Server stopped (exit {proc.ExitCode}) — restarting in {s}s...   {Rst}");
                await Task.Delay(1000);
            }
            Console.WriteLine();
            Console.WriteLine($"{O}{sep}{Rst}");
            Console.WriteLine();
        }
    }

    static async Task DownloadAndExtract(HttpClient http, string url, string dest, string label)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1L;
        await using var netStream = await response.Content.ReadAsStreamAsync();
        using var buf = new MemoryStream(total > 0 ? (int)Math.Min(total, int.MaxValue) : 4 * 1024 * 1024);

        var chunk = new byte[65536];
        long downloaded = 0;
        long speedBase = 0;
        double speed = 0;
        var speedTimer = Stopwatch.StartNew();

        int read;
        while ((read = await netStream.ReadAsync(chunk)) > 0)
        {
            buf.Write(chunk, 0, read);
            downloaded += read;

            if (speedTimer.Elapsed.TotalMilliseconds > 250)
            {
                speed = (downloaded - speedBase) / speedTimer.Elapsed.TotalSeconds;
                speedBase = downloaded;
                speedTimer.Restart();
            }

            DrawBar(label, downloaded, total, speed, false);
        }

        DrawBar(label, downloaded, total, 0, true);
        Console.WriteLine();

        buf.Position = 0;
        Directory.CreateDirectory(dest);
        using var zip = new ZipArchive(buf, ZipArchiveMode.Read);
        zip.ExtractToDirectory(dest, overwriteFiles: true);
    }

    static void DrawBar(string label, long bytes, long total, double speed, bool done)
    {
        const int W = 20;
        double pct = total > 0 ? (double)bytes / total : 0;
        int filled = (int)(pct * W);

        Console.Write($"\r    {label,-6}  [");
        Console.ForegroundColor = done ? ConsoleColor.Green : ConsoleColor.Cyan;
        Console.Write(new string('█', filled));
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('░', W - filled));
        Console.ResetColor();
        Console.Write("]");

        if (total > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  {pct * 100,3:F0}%");
            Console.ResetColor();
        }

        string sizeStr = total > 0 ? $"  {Fmt(bytes)}/{Fmt(total)}" : $"  {Fmt(bytes)}";
        Console.Write($"{sizeStr,-16}");

        if (done)
        {
            Console.Write("          ");
        }
        else if (speed > 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"{Fmt((long)speed)}/s  ");
            Console.ResetColor();
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("...       ");
            Console.ResetColor();
        }
    }

    static string Fmt(long b) => b switch
    {
        < 1024 => $"{b} B",
        < 1024 * 1024 => $"{b / 1024.0:F1} KB",
        _ => $"{b / (1024.0 * 1024):F1} MB",
    };

    static void Banner()
    {
        const string B = "\x1b[38;2;57;76;210m";
        const string R = "\x1b[0m";
        string[] rows =
        [
            "  ██████╗ ██╗  ██╗██████╗ ██████╗ ",
            "  ██╔══██╗╚██╗██╔╝██╔══██╗██╔══██╗",
            "  ██║  ██║ ╚███╔╝ ██████╔╝██████╔╝",
            "  ██║  ██║ ██╔██╗ ██╔══██╗██╔═══╝ ",
            "  ██████╔╝██╔╝ ██╗██║  ██║██║     ",
            "  ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝╚═╝     ",
        ];
        Console.WriteLine();
        foreach (var row in rows) Console.WriteLine($"{B}{row}{R}");
        const string subtitle = "Server Launcher";
        int pad = (rows[0].Length - subtitle.Length) / 2;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string(' ', pad) + subtitle);
        Console.WriteLine();
        Console.ResetColor();
    }

    static void Header(int step, int total, string name)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"\n  [{step}/{total}]  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(name);
        Console.ResetColor();
    }

    static void Info(string msg)
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"  {msg}");
        Console.ResetColor();
    }

    static void Ok(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  ✓  ");
        Console.ResetColor();
        Console.WriteLine(msg);
    }

    static void Fatal(string msg)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n  ✗  {msg}");
        Console.ResetColor();
        Environment.Exit(1);
    }

    static string Mask(string t) =>
        t.Length <= 8 ? "****" : t[..4] + new string('*', Math.Max(t.Length - 8, 4)) + t[^4..];

    static void RunGit(string[] gitArgs)
    {
        var psi = new ProcessStartInfo("git")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var a in gitArgs) psi.ArgumentList.Add(a);
        var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0) Fatal($"git exited with code {p.ExitCode}.");
    }

    static void BuildOrFatal()
    {
        var psi = new ProcessStartInfo("dotnet", $"build \"{Path.GetFullPath(Path.Combine(RepoDir, "game", "Code", "rp.csproj"))}\" --nologo -v quiet")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        var p = Process.Start(psi)!;

        var lines = new System.Collections.Concurrent.ConcurrentQueue<string>();
        p.OutputDataReceived += (_, e) => { if (e.Data != null) lines.Enqueue(e.Data); };
        p.ErrorDataReceived  += (_, e) => { if (e.Data != null) lines.Enqueue(e.Data); };
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        char[] spinner = ['⠋','⠙','⠹','⠸','⠼','⠴','⠦','⠧','⠇','⠏'];
        int frame = 0;
        while (!p.WaitForExit(80))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"\r    {spinner[frame++ % spinner.Length]}  compiling...");
            Console.ResetColor();
        }
        p.WaitForExit();

        if (p.ExitCode != 0)
        {
            Console.Write("\r" + new string(' ', 30) + "\r");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗  build failed\n");
            Console.ResetColor();

            var errorRe = new System.Text.RegularExpressions.Regex(
                @"^(?:.*[\\/])?(.+?)\((\d+),\d+\): error (CS\w+): (.+?)(?:\s+\[.+\])?$");

            foreach (var line in lines)
            {
                var m = errorRe.Match(line);
                if (!m.Success) continue;
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write($"    {m.Groups[1].Value}:{m.Groups[2].Value}  ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(m.Groups[3].Value);
                Console.ResetColor();
                Console.WriteLine($"  {m.Groups[4].Value}");
            }

            Console.WriteLine();
            Environment.Exit(1);
        }

        Console.Write("\r" + new string(' ', 30) + "\r");
        Ok("compiled");
    }

    static void ClearDir(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var d in Directory.GetDirectories(path)) Directory.Delete(d, true);
    }

    static Config LoadConfig()
    {
        if (File.Exists(ConfigFile))
        {
            try { return JsonSerializer.Deserialize(File.ReadAllText(ConfigFile), AppJsonContext.Default.Config) ?? new Config(); }
            catch { }
        }
        var c = new Config();
        SaveConfig(c);
        return c;
    }

    static void SaveConfig(Config c) =>
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(c, AppJsonContext.Default.Config));
}

[JsonSerializable(typeof(Config))]
[JsonSerializable(typeof(List<AddonInfo>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
internal partial class AppJsonContext : JsonSerializerContext { }

class Config
{
    [JsonPropertyName("token")]       public string Token          { get; set; } = "";
    [JsonPropertyName("repoUrl")]     public string RepoUrl        { get; set; } = "https://github.com/dxura/dxrp.git";
    [JsonPropertyName("branch")]      public string Branch         { get; set; } = "main";
    [JsonPropertyName("apiEndpoint")] public string ApiEndpoint    { get; set; } = "https://api.dxrp.net";
    [JsonPropertyName("verifyAddons")] public bool   VerifyAddons   { get; set; } = true;
    [JsonPropertyName("map")]         public string Map            { get; set; } = "";
    [JsonPropertyName("extraArgs")]   public string ExtraArgs      { get; set; } = "";
}

class AddonInfo
{
    [JsonPropertyName("addonId")]           public string  AddonId           { get; set; } = "";
    [JsonPropertyName("addonName")]         public string  AddonName         { get; set; } = "";
    [JsonPropertyName("addonIdentifier")]   public string  AddonIdentifier   { get; set; } = "";
    [JsonPropertyName("networkIdentifier")] public string  NetworkIdentifier { get; set; } = "";
    [JsonPropertyName("revisionNumber")]    public int     RevisionNumber    { get; set; }
    [JsonPropertyName("sboxVersion")]       public string? SboxVersion       { get; set; }
    [JsonPropertyName("codePackageUrl")]    public string? CodePackageUrl    { get; set; }
    [JsonPropertyName("assetsPackageUrl")]  public string? AssetsPackageUrl  { get; set; }
}
