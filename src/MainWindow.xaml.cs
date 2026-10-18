using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NovaStrap;

public partial class MainWindow : Window
{
    private readonly string dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NovaStrap");
    private List<GameEntry> games = new();
    private AppSettings appSettings = new();
    private string theme = "Midnight";
    private string accent = "#8B7CFF";
    private bool installed;
    private bool updatingSettings;

    private string GamesFile => Path.Combine(dataDir, "games.json");
    private string SettingsFile => Path.Combine(dataDir, "settings.json");
    private string LogFile => Path.Combine(dataDir, "novastrap.log");

    public MainWindow()
    {
        InitializeComponent();
        Directory.CreateDirectory(dataDir);
        LoadData();
        ApplyWindowSettings();
        installed = DetectRoblox();
        UpdateStatus();
        LogLocal("NovaStrap started.");
        ShowPage(appSettings.RememberLastPage && KnownPage(appSettings.LastPage) ? appSettings.LastPage : "Home");
        Closing += (_, _) =>
        {
            if (WindowState == WindowState.Normal)
            {
                appSettings.WindowWidth = Math.Max(950, Width);
                appSettings.WindowHeight = Math.Max(620, Height);
            }
            SaveData(showErrors: false);
        };
    }

    private static bool KnownPage(string? p) => p is "Home" or "Launch" or "Game Library" or "Client Settings" or "FastFlags" or "Mod Manager" or "Cursor Studio" or "Performance" or "Discord" or "Themes" or "Diagnostics" or "Settings" or "About";

    private void LoadData()
    {
        try
        {
            if (File.Exists(GamesFile))
                games = JsonSerializer.Deserialize<List<GameEntry>>(File.ReadAllText(GamesFile)) ?? new();
        }
        catch { games = new(); }

        try
        {
            if (File.Exists(SettingsFile))
                appSettings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsFile)) ?? new AppSettings();
        }
        catch { appSettings = new AppSettings(); }

        theme = string.IsNullOrWhiteSpace(appSettings.Theme) ? "Midnight" : appSettings.Theme;
        accent = string.IsNullOrWhiteSpace(appSettings.Accent) ? "#8B7CFF" : appSettings.Accent;
        ApplyTheme();
    }

    private void SaveData(bool showErrors = true)
    {
        try
        {
            appSettings.Theme = theme;
            appSettings.Accent = accent;
            File.WriteAllText(GamesFile, JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true }));
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(appSettings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception e)
        {
            LogLocal("Could not save settings: " + e.Message);
            if (showErrors) MessageBox.Show(e.Message, "NovaStrap · Save failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LogLocal(string message)
    {
        if (!appSettings.EnableLocalLogging) return;
        try
        {
            Directory.CreateDirectory(dataDir);
            File.AppendAllText(LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { /* Logging must never prevent normal app usage. */ }
    }

    private bool DetectRoblox()
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions");
        try
        {
            return Directory.Exists(path) && Directory.GetDirectories(path).Any(d => File.Exists(Path.Combine(d, "RobloxPlayerBeta.exe")));
        }
        catch { return false; }
    }

    private void UpdateStatus()
    {
        TopStatus.Text = installed ? "Roblox detected" : "Roblox not detected";
        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(installed ? "#58D6A0" : "#F5B94C"));
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string page) ShowPage(page);
    }

    private Border Panel(UIElement content) => new()
    {
        Background = (Brush)FindResource("PanelBrush"),
        CornerRadius = new CornerRadius(15),
        Padding = new Thickness(19),
        Margin = new Thickness(0, 0, 0, 14),
        Child = content,
        BorderBrush = new SolidColorBrush(Color.FromRgb(35, 42, 57)),
        BorderThickness = new Thickness(1)
    };

    private TextBlock H(string text, double size = 17) => new()
    {
        Text = text,
        FontSize = size,
        FontWeight = FontWeights.SemiBold,
        Foreground = (Brush)FindResource("TextBrush"),
        Margin = new Thickness(0, 0, 0, 8)
    };

    private TextBlock M(string text) => new()
    {
        Text = text,
        Foreground = (Brush)FindResource("MutedBrush"),
        TextWrapping = TextWrapping.Wrap,
        FontSize = 12,
        Margin = new Thickness(0, 2, 0, 8)
    };

    private Button B(string text, Action action, bool primary = false)
    {
        var button = new Button
        {
            Content = text,
            Background = primary ? (Brush)FindResource("AccentBrush") : (Brush)FindResource("Panel2Brush"),
            HorizontalAlignment = HorizontalAlignment.Left,
            MinWidth = 110
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void Add(UIElement element) => Content.Children.Add(element);

    private void ShowPage(string page)
    {
        if (appSettings.AutoRefreshRobloxStatus && (page is "Home" or "Launch"))
        {
            installed = DetectRoblox();
            UpdateStatus();
        }

        Content.Children.Clear();
        PageTitle.Text = page == "Home" ? "Welcome back." : page;
        PageSubtitle.Text = page switch
        {
            "Home" => "Your Roblox setup, one calm place.",
            "Launch" => "Launch Roblox using standard Windows mechanisms.",
            "Game Library" => "Keep favourite experiences one click away.",
            "Diagnostics" => "Transparent checks of the local installation.",
            "Settings" => "Personalise how NovaStrap behaves on this PC.",
            _ => "Thoughtful controls, clear status, and local-first preferences."
        };

        switch (page)
        {
            case "Home": Home(); break;
            case "Launch": Launch(); break;
            case "Game Library": Library(); break;
            case "Client Settings": ClientSettings(); break;
            case "FastFlags": FastFlags(); break;
            case "Mod Manager": Info("Safe cosmetic management", "This build does not patch Roblox files, run imported executables, inject DLLs, or overwrite client files. Choose an image to inspect it without applying it.", InspectFile); break;
            case "Cursor Studio": Cursor(); break;
            case "Performance": Performance(); break;
            case "Discord": Info("Discord Rich Presence", "Discord Rich Presence is not active in this build. No Discord data is collected and no fabricated presence is displayed."); break;
            case "Themes": Themes(); break;
            case "Diagnostics": Diagnostics(); break;
            case "Settings": Settings(); break;
            case "About": Info("NovaStrap", "Independent third-party Roblox launcher companion. Not affiliated with Roblox Corporation. Version 0.2.0. Some controls are guidance-only and are labelled honestly.", () => OpenUrl("https://github.com/bloxstraplabs/bloxstrap")); break;
        }

        appSettings.LastPage = page;
        SaveData(showErrors: false);
    }

    private void Home()
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = "YOUR GAMES. YOUR SETUP.", FontSize = 10, FontWeight = FontWeights.Bold, Foreground = (Brush)FindResource("AccentBrush") });
        stack.Children.Add(H(installed ? "Ready when you are." : "Let's get you playing.", 29));
        stack.Children.Add(M(installed ? "Roblox client files were found on this PC. No password or cookie is needed." : "We couldn't find a standard Roblox client installation. Install it from the official page."));
        stack.Children.Add(B(installed ? "▶  Play Roblox" : "Install Roblox", () => { if (installed) LaunchRoblox(); else OpenUrl("https://www.roblox.com/download"); }, true));
        stack.Children.Add(B("Open Roblox Website", () => OpenUrl("https://www.roblox.com/")));
        Add(Panel(stack));

        var quick = new StackPanel();
        quick.Children.Add(H("Quick access"));
        quick.Children.Add(M($"{games.Count} saved game shortcut(s) · settings stored locally"));
        quick.Children.Add(B("Game Library", () => ShowPage("Game Library")));
        quick.Children.Add(B("Customise theme", () => ShowPage("Themes")));
        quick.Children.Add(B("Configure launcher behaviour", () => ShowPage("Settings"), true));
        quick.Children.Add(B("Run diagnostics", () => ShowPage("Diagnostics")));
        Add(Panel(quick));

        var activity = new StackPanel();
        activity.Children.Add(H("Privacy & activity"));
        activity.Children.Add(M(appSettings.EnableLocalLogging ? $"Local diagnostics logging is enabled. Log file: {LogFile}" : "Local diagnostics logging is disabled. No activity is sent to a server."));
        Add(Panel(activity));
    }

    private void Launch()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("Launch Roblox"));
        stack.Children.Add(M(installed ? "Client installation detected." : "Roblox is not detected. Use the official installer first."));
        stack.Children.Add(M($"Launch confirmation: {(appSettings.ConfirmBeforeLaunch ? "On" : "Off")} · Close launcher after launch: {(appSettings.CloseAfterLaunching ? "On" : "Off")}"));
        stack.Children.Add(B(installed ? "▶ Launch Roblox" : "Open official install page", () => { if (installed) LaunchRoblox(); else OpenUrl("https://www.roblox.com/download"); }, true));
        stack.Children.Add(B("Change launch behaviour", () => ShowPage("Settings")));
        stack.Children.Add(H("Quick launch a game", 14));
        var input = new TextBox();
        stack.Children.Add(input);
        stack.Children.Add(M("Paste a Roblox game URL or numeric place ID."));
        stack.Children.Add(B("Launch game link", () =>
        {
            var id = PlaceId(input.Text);
            if (id == null) { MessageBox.Show("Enter a valid roblox.com/games/ URL or numeric place ID."); return; }
            if (appSettings.ConfirmBeforeLaunch && MessageBox.Show($"Open Roblox game {id} in your browser / Roblox app?", "Confirm game launch", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            LogLocal("Opened game link for place ID " + id);
            OpenUrl("https://www.roblox.com/games/" + id);
            if (appSettings.CloseAfterLaunching) Close();
        }, true));
        Add(Panel(stack));
    }

    private void Library()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("Add a favourite"));
        var name = new TextBox { Text = "My Roblox game" };
        var url = new TextBox();
        stack.Children.Add(M("Game title")); stack.Children.Add(name);
        stack.Children.Add(M("Roblox URL or place ID")); stack.Children.Add(url);
        stack.Children.Add(B("＋ Save game", () =>
        {
            var id = PlaceId(url.Text);
            if (id == null) { MessageBox.Show("Enter a valid Roblox game URL or numeric place ID."); return; }
            if (games.Any(g => g.PlaceId == id)) { MessageBox.Show("That game is already in your library."); return; }
            games.Add(new GameEntry { Title = string.IsNullOrWhiteSpace(name.Text) ? "Roblox game" : name.Text.Trim(), PlaceId = id });
            LogLocal("Added game shortcut " + id);
            SaveData(); ShowPage("Game Library");
        }, true));
        Add(Panel(stack));

        if (games.Count == 0)
        {
            Add(Panel(new StackPanel { Children = { H("Your library is empty"), M("Add a game above to create a quick-launch shortcut.") } }));
            return;
        }

        foreach (var game in games.ToList())
        {
            var item = new StackPanel();
            item.Children.Add(H(game.Title));
            item.Children.Add(M("Place ID: " + game.PlaceId));
            item.Children.Add(B("▶ Launch", () =>
            {
                if (appSettings.ConfirmBeforeLaunch && MessageBox.Show($"Open '{game.Title}'?", "Confirm game launch", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
                LogLocal("Opened saved game " + game.PlaceId);
                OpenUrl("https://www.roblox.com/games/" + game.PlaceId);
                if (appSettings.CloseAfterLaunching) Close();
            }, true));
            item.Children.Add(B("Remove", () =>
            {
                if (MessageBox.Show("Remove this shortcut?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                { games.Remove(game); SaveData(); ShowPage("Game Library"); }
            }));
            Add(Panel(item));
        }
    }

    private void ClientSettings()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("Client settings"));
        stack.Children.Add(M("NovaStrap keeps client actions transparent. It does not inject into Roblox or write undocumented client flags."));
        stack.Children.Add(B("Open Roblox local folder", () =>
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox");
            if (Directory.Exists(path)) OpenPath(path); else MessageBox.Show("Roblox folder not found.");
        }));
        stack.Children.Add(B("Re-check installation", () => { installed = DetectRoblox(); UpdateStatus(); MessageBox.Show(installed ? "Roblox client detected." : "Roblox client not found in the standard location."); }));
        stack.Children.Add(B("Open Windows installed-app settings", () => OpenUrl("ms-settings:appsfeatures")));
        stack.Children.Add(M("If Roblox is not detected, repair or reinstall it using the official download page."));
        stack.Children.Add(B("Official Roblox download", () => OpenUrl("https://www.roblox.com/download"), true));
        Add(Panel(stack));
    }

    private void FastFlags()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("FastFlags & client configuration"));
        stack.Children.Add(M("Roblox restricts which client FastFlags are accepted. This NovaStrap build does not write arbitrary or undocumented flags, and it will not claim a setting is active when Roblox may ignore it."));
        stack.Children.Add(M("For reliability, use Roblox's in-game settings for graphics and supported preferences. This page is informational; it does not apply configuration changes."));
        stack.Children.Add(B("Open Roblox local folder", () => OpenPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox"))));
        stack.Children.Add(B("View official Bloxstrap source", () => OpenUrl("https://github.com/bloxstraplabs/bloxstrap"), true));
        Add(Panel(stack));
    }

    private void Cursor()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("Cursor Studio"));
        stack.Children.Add(M("Windows pointer customisation is separate from Roblox's in-game cursor. This page does not claim to replace the in-game cursor."));
        stack.Children.Add(B("Open Windows mouse pointer settings", () => OpenUrl("ms-settings:easeofaccess-mousepointer"), true));
        stack.Children.Add(B("Open Mouse Properties", () =>
        {
            try { Process.Start(new ProcessStartInfo("control.exe", "main.cpl") { UseShellExecute = true }); }
            catch (Exception e) { MessageBox.Show(e.Message); }
        }));
        stack.Children.Add(M("No system-wide pointer changes are made without your action."));
        Add(Panel(stack));
    }

    private void Performance()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("Performance & graphics"));
        stack.Children.Add(M("Choose a saved recommendation profile. These tips do not change Roblox's graphics settings automatically."));
        var options = new[] { "Balanced", "Performance guidance", "Visual quality guidance", "Low-end PC guidance" };
        var combo = new ComboBox { ItemsSource = options, SelectedItem = appSettings.PerformanceProfile, Margin = new Thickness(0, 6, 0, 8) };
        var detail = M(PerformanceDescription(combo.SelectedItem?.ToString()));
        combo.SelectionChanged += (_, _) =>
        {
            appSettings.PerformanceProfile = combo.SelectedItem?.ToString() ?? "Balanced";
            detail.Text = PerformanceDescription(appSettings.PerformanceProfile);
            SaveData(showErrors: false);
        };
        stack.Children.Add(combo); stack.Children.Add(detail);
        stack.Children.Add(B("Open Windows graphics settings", () => OpenUrl("ms-settings:display-advancedgraphics")));
        stack.Children.Add(B("Open Windows power settings", () => OpenUrl("ms-settings:powersleep")));
        Add(Panel(stack));
    }

    private static string PerformanceDescription(string? profile) => profile switch
    {
        "Performance guidance" => "Lower in-game graphics quality, close unnecessary apps, and use a stable power mode.",
        "Visual quality guidance" => "Raise in-game graphics quality only while frame rate remains stable.",
        "Low-end PC guidance" => "Use lower graphics quality and reduce background workload.",
        _ => "Balanced: start with Roblox automatic graphics mode and adjust in-game."
    };

    private void Themes()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("Appearance"));
        var themes = new[] { "Midnight", "Arctic", "Cyber Neon", "Crimson", "Ocean", "macOS-inspired" };
        var combo = new ComboBox { ItemsSource = themes, SelectedItem = theme, Margin = new Thickness(0, 6, 0, 8) };
        stack.Children.Add(M("Theme")); stack.Children.Add(combo);
        var accents = new Dictionary<string, string>
        {
            ["Violet"] = "#8B7CFF", ["Blue"] = "#56B8FF", ["Mint"] = "#44E7C5", ["Pink"] = "#FF5E78", ["Ocean"] = "#3D9BFF", ["Slate"] = "#8EA4C8"
        };
        var currentAccent = accents.FirstOrDefault(p => p.Value.Equals(accent, StringComparison.OrdinalIgnoreCase)).Key ?? "Violet";
        var accentCombo = new ComboBox { ItemsSource = accents.Keys.ToArray(), SelectedItem = currentAccent, Margin = new Thickness(0, 6, 0, 8) };
        stack.Children.Add(M("Accent colour")); stack.Children.Add(accentCombo);
        stack.Children.Add(B("Apply appearance", () =>
        {
            theme = combo.SelectedItem?.ToString() ?? "Midnight";
            accent = accents.GetValueOrDefault(accentCombo.SelectedItem?.ToString() ?? "Violet", "#8B7CFF");
            ApplyTheme(); SaveData(); ShowPage("Themes");
        }, true));
        stack.Children.Add(M("Appearance changes affect NovaStrap only, not Roblox itself."));
        Add(Panel(stack));
    }

    private void Diagnostics()
    {
        var stack = new StackPanel();
        stack.Children.Add(H("Local diagnostics"));
        stack.Children.Add(M("Operating system: " + Environment.OSVersion));
        stack.Children.Add(M("Runtime: " + Environment.Version));
        stack.Children.Add(M("64-bit process: " + Environment.Is64BitProcess));
        stack.Children.Add(M("Roblox detected: " + installed));
        stack.Children.Add(M("Checked path: " + Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions")));
        stack.Children.Add(M("NovaStrap data: " + dataDir));
        stack.Children.Add(M("Local log: " + (appSettings.EnableLocalLogging ? LogFile : "disabled")));
        stack.Children.Add(B("Refresh checks", () => { installed = DetectRoblox(); UpdateStatus(); ShowPage("Diagnostics"); }, true));
        stack.Children.Add(B("Export diagnostics to text file", ExportDiagnostics));
        stack.Children.Add(B("Open app data folder", () => OpenPath(dataDir)));
        if (File.Exists(LogFile)) stack.Children.Add(B("Open local log", () => OpenPath(LogFile)));
        stack.Children.Add(B("Official Roblox support", () => OpenUrl("https://www.roblox.com/support")));
        Add(Panel(stack));
    }

    private void ExportDiagnostics()
    {
        var dialog = new SaveFileDialog { Title = "Export NovaStrap diagnostics", FileName = "NovaStrap-Diagnostics.txt", Filter = "Text files|*.txt", DefaultExt = ".txt" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var report = $"NovaStrap diagnostics\r\nTime: {DateTime.Now:O}\r\nOS: {Environment.OSVersion}\r\nRuntime: {Environment.Version}\r\n64-bit process: {Environment.Is64BitProcess}\r\nRoblox detected: {installed}\r\nRoblox folder: {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions")}\r\nNovaStrap data folder: {dataDir}\r\nTheme: {theme}\r\nLaunch confirmation: {appSettings.ConfirmBeforeLaunch}\r\nClose after launch: {appSettings.CloseAfterLaunching}\r\nStartup registration: {appSettings.StartWithWindows}\r\n";
            File.WriteAllText(dialog.FileName, report);
            MessageBox.Show("Diagnostics exported. Review the file before sharing it because it contains local system details.", "Export complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception e) { MessageBox.Show("Could not export diagnostics: " + e.Message); }
    }

    private void Settings()
    {
        updatingSettings = true;
        var stack = new StackPanel();
        stack.Children.Add(H("General behaviour"));
        stack.Children.Add(M("These preferences are saved in your Windows user profile under " + dataDir));
        stack.Children.Add(Toggle("Confirm before launching", "Show a confirmation prompt before opening Roblox or a saved game.", appSettings.ConfirmBeforeLaunch, value => { appSettings.ConfirmBeforeLaunch = value; SaveData(false); LogLocal("Confirm-before-launch set to " + value); }));
        stack.Children.Add(Toggle("Close NovaStrap after launching", "Closes this window after sending a launch request to Roblox.", appSettings.CloseAfterLaunching, value => { appSettings.CloseAfterLaunching = value; SaveData(false); }));
        stack.Children.Add(Toggle("Refresh Roblox detection automatically", "Re-check the local Roblox installation when opening Home or Launch.", appSettings.AutoRefreshRobloxStatus, value => { appSettings.AutoRefreshRobloxStatus = value; SaveData(false); if (value) { installed = DetectRoblox(); UpdateStatus(); } }));
        stack.Children.Add(Toggle("Remember the last page", "Reopen the last NovaStrap page when the app starts.", appSettings.RememberLastPage, value => { appSettings.RememberLastPage = value; SaveData(false); }));
        stack.Children.Add(Toggle("Always keep NovaStrap on top", "Keep the NovaStrap window above normal windows.", appSettings.AlwaysOnTop, value => { appSettings.AlwaysOnTop = value; Topmost = value; SaveData(false); }));
        stack.Children.Add(Toggle("Compact window", "Use a smaller window layout. You can turn this off here at any time.", appSettings.CompactMode, value => { appSettings.CompactMode = value; ApplyWindowSettings(); SaveData(false); }));
        stack.Children.Add(Toggle("Start with Windows", "Add or remove NovaStrap from your current user's Windows startup apps.", appSettings.StartWithWindows, value =>
        {
            if (SetStartupRegistration(value, out var error))
            {
                appSettings.StartWithWindows = value; SaveData(false); LogLocal("Start with Windows set to " + value);
            }
            else
            {
                MessageBox.Show(error, "Startup setting failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                appSettings.StartWithWindows = !value;
                ShowPage("Settings");
            }
        }));

        stack.Children.Add(H("Privacy & diagnostics", 15));
        stack.Children.Add(Toggle("Enable local diagnostic log", "Keep a small text log on this PC. It is not uploaded by NovaStrap.", appSettings.EnableLocalLogging, value => { appSettings.EnableLocalLogging = value; SaveData(false); if (value) LogLocal("Local logging enabled."); }));
        stack.Children.Add(B("Open settings folder", () => OpenPath(dataDir)));
        stack.Children.Add(B("Open settings JSON", () => { if (File.Exists(SettingsFile)) OpenPath(SettingsFile); else MessageBox.Show("Settings file has not been created yet."); }));
        stack.Children.Add(B("Export diagnostics", ExportDiagnostics));
        stack.Children.Add(H("Reset", 15));
        stack.Children.Add(M("Reset restores NovaStrap preferences and removes its optional Windows startup entry. Your saved game shortcuts are kept."));
        stack.Children.Add(B("Reset preferences to defaults", () =>
        {
            if (MessageBox.Show("Reset NovaStrap preferences? Saved games will be kept.", "Reset preferences", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
            SetStartupRegistration(false, out _);
            appSettings = new AppSettings(); theme = "Midnight"; accent = "#8B7CFF";
            ApplyTheme(); ApplyWindowSettings(); SaveData(); ShowPage("Settings");
        }, true));
        Add(Panel(stack));
        updatingSettings = false;
    }

    private UIElement Toggle(string title, string detail, bool value, Action<bool> changed)
    {
        var block = new StackPanel { Margin = new Thickness(0, 5, 0, 10) };
        var check = new CheckBox { Content = title, IsChecked = value, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("TextBrush"), Margin = new Thickness(0, 3, 0, 2) };
        check.Checked += (_, _) => { if (!updatingSettings) changed(true); };
        check.Unchecked += (_, _) => { if (!updatingSettings) changed(false); };
        block.Children.Add(check);
        block.Children.Add(new TextBlock { Text = detail, Foreground = (Brush)FindResource("MutedBrush"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(23, 1, 0, 0) });
        return block;
    }

    private void ApplyWindowSettings()
    {
        Topmost = appSettings.AlwaysOnTop;
        if (appSettings.CompactMode)
        {
            Width = 1000; Height = 700;
        }
        else
        {
            Width = Math.Clamp(appSettings.WindowWidth, 950, 1800);
            Height = Math.Clamp(appSettings.WindowHeight, 620, 1200);
        }
    }

    private bool SetStartupRegistration(bool enabled, out string error)
    {
        error = string.Empty;
        const string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(runKey, writable: true) ?? Registry.CurrentUser.CreateSubKey(runKey, writable: true);
            if (enabled)
            {
                var executable = Environment.ProcessPath;
                if (string.IsNullOrWhiteSpace(executable) || !File.Exists(executable))
                    throw new InvalidOperationException("The running executable path could not be determined. Run the published NovaStrap.exe to enable startup.");
                key.SetValue("NovaStrap", $"\"{executable}\"");
            }
            else key.DeleteValue("NovaStrap", throwOnMissingValue: false);
            return true;
        }
        catch (Exception e) { error = "Windows could not update the current-user startup entry. " + e.Message; return false; }
    }

    private void Info(string title, string description, Action? extra = null)
    {
        var stack = new StackPanel(); stack.Children.Add(H(title)); stack.Children.Add(M(description));
        if (extra != null) stack.Children.Add(B("Open reference", extra));
        Add(Panel(stack));
    }

    private void InspectFile()
    {
        var dialog = new OpenFileDialog { Title = "Select an image to inspect", Filter = "Image files|*.png;*.jpg;*.jpeg;*.webp|All files|*.*" };
        if (dialog.ShowDialog() == true) MessageBox.Show("Selected file:\n" + dialog.FileName + "\n\nNo file was applied or executed.", "Inspection only");
    }

    private void ApplyTheme()
    {
        try
        {
            Resources["AccentBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accent));
            Resources["PanelBrush"] = new SolidColorBrush(theme == "Arctic" ? Color.FromRgb(22, 31, 43) : Color.FromRgb(21, 25, 34));
            Background = new SolidColorBrush(theme == "Arctic" ? Color.FromRgb(13, 17, 24) : Color.FromRgb(12, 14, 19));
        }
        catch { }
    }

    private void LaunchRoblox()
    {
        if (appSettings.ConfirmBeforeLaunch && MessageBox.Show("Launch the installed Roblox Player?", "Confirm launch", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            var versions = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Roblox", "Versions");
            var executable = Directory.Exists(versions)
                ? Directory.GetDirectories(versions).Select(d => Path.Combine(d, "RobloxPlayerBeta.exe")).Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()
                : null;
            if (executable == null)
            {
                installed = false; UpdateStatus(); MessageBox.Show("Roblox executable not found. Use the official installer to repair it."); return;
            }
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = true });
            LogLocal("Launched Roblox Player.");
            if (appSettings.CloseAfterLaunching) Close();
        }
        catch (Exception e) { MessageBox.Show("Launch failed: " + e.Message + "\nTry the official Roblox installer."); }
    }

    private static string? PlaceId(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var text = input.Trim();
        if (Regex.IsMatch(text, @"^\d{1,20}$")) return text;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (!uri.Host.Equals("roblox.com", StringComparison.OrdinalIgnoreCase) && !uri.Host.EndsWith(".roblox.com", StringComparison.OrdinalIgnoreCase))) return null;
        var match = Regex.Match(uri.AbsolutePath, @"/games/(\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value : null;
    }

    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception e) { MessageBox.Show("Could not open link: " + e.Message); }
    }

    private static void OpenPath(string path)
    {
        try
        {
            if (File.Exists(path) || Directory.Exists(path)) Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            else MessageBox.Show("Path not found: " + path);
        }
        catch (Exception e) { MessageBox.Show(e.Message); }
    }

    private sealed class GameEntry
    {
        public GameEntry() { }
        public string Title { get; set; } = "Roblox game";
        public string PlaceId { get; set; } = "";
    }

    private sealed class AppSettings
    {
        public AppSettings() { }
        public string Theme { get; set; } = "Midnight";
        public string Accent { get; set; } = "#8B7CFF";
        public bool ConfirmBeforeLaunch { get; set; } = true;
        public bool CloseAfterLaunching { get; set; }
        public bool AutoRefreshRobloxStatus { get; set; } = true;
        public bool RememberLastPage { get; set; } = true;
        public bool AlwaysOnTop { get; set; }
        public bool CompactMode { get; set; }
        public bool StartWithWindows { get; set; }
        public bool EnableLocalLogging { get; set; } = true;
        public string PerformanceProfile { get; set; } = "Balanced";
        public string LastPage { get; set; } = "Home";
        public double WindowWidth { get; set; } = 1200;
        public double WindowHeight { get; set; } = 780;
    }
}
