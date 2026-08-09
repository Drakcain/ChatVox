using System.Net.Http;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ChatVox.Filtering;
using ChatVox.Logging;
using ChatVox.Queue;
using ChatVox.Runtime;
using ChatVox.Settings;
using ChatVox.Speech;
using ChatVox.Twitch;
using ChatVox.Updates;
using ChatVox.Windowing;
using Forms = System.Windows.Forms;
using Clipboard = System.Windows.Clipboard;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace ChatVox;

public partial class MainWindow : Window
{
    private readonly FreshQueue queue = new();
    private readonly ChatFilter filter = new();
    private readonly EventDeduplicator dedup = new();
    private readonly KokoroService kokoro = new();
    private readonly HttpClient http = new();
    private readonly AppSettingsStore settingsStore = new();
    private readonly AppSettings settings;
    private readonly ChatPipeline pipeline;
    private readonly SpeechWorker worker;
    private readonly Queue<string> diagnosticLines = new();
    private readonly OperationalLog log = new();
    private readonly TwitchHealth health = new();
    private readonly CancellationTokenSource appStopping = new();
    private readonly WindowsStartupService startupService = new();
    private readonly LaunchReason launchReason;
    private readonly bool startHidden;
    private readonly SemaphoreSlim twitchGate = new(1, 1);
    private EventSubClient? eventSub;
    private CancellationTokenSource? twitchRun;
    private Task? tokenMonitorTask;
    private TokenResponse? activeAuth;
    private TokenValidation? activeIdentity;
    private bool paused, loading = true;
    private bool allowExit, cleanupStarted;
    private WindowState lastVisibleWindowState = WindowState.Normal;
    private int startupRestoreAttempt;
    private Forms.NotifyIcon? trayIcon;
    private Forms.ToolStripMenuItem? trayPauseItem;
    private UpdateCheckResult? pendingUpdate;
    private string version = "1.0.0-rc.8";

    public MainWindow(LaunchReason launchReason = LaunchReason.Normal)
    {
        InitializeComponent();
        this.launchReason = launchReason;
        version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
        AboutVersion.Text = "Version " + version;
        log.Write("APPLICATION", "startup version " + version);
        settings = settingsStore.Load();
        if (settings.MaxAgeSeconds == 10) { settings.MaxAgeSeconds = 30; settingsStore.Save(settings); }
        pipeline = new ChatPipeline(filter, queue, dedup);
        queue.Expired += count => AppendDiagnostic($"queue expired {count} stale item(s)");
        var voicesView = CollectionViewSource.GetDefaultView(Voices.All);
        voicesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(VoiceOption.Category)));
        voicesView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(VoiceOption.CategoryOrder), System.ComponentModel.ListSortDirection.Ascending));
        voicesView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(VoiceOption.WithinCategoryOrder), System.ComponentModel.ListSortDirection.Ascending));
        voicesView.SortDescriptions.Add(new System.ComponentModel.SortDescription(nameof(VoiceOption.FriendlyName), System.ComponentModel.ListSortDirection.Ascending));
        Voice.ItemsSource = voicesView;
        LoadSettings();
        startHidden = StartupVisibilityPolicy.ShouldStartHidden(settings, launchReason);
        kokoro.Diagnostic += AppendDiagnostic;
        worker = new SpeechWorker(queue, kokoro, () => (settings.Voice, (float)settings.Speed, (float)settings.Volume, settings.SpeechGapMilliseconds), () => paused);
        worker.Diagnostic += AppendDiagnostic;
        worker.StateChanged += () => Dispatcher.BeginInvoke(UpdateStatus);
        worker.Diagnostic += line => { if (line.Contains("exception", StringComparison.OrdinalIgnoreCase) || line.Contains("stopped", StringComparison.OrdinalIgnoreCase)) log.Write("WORKER", line); };
        AppendDiagnostic($"queue/speech worker ready; launch={launchReason}; start hidden={startHidden}");
        InitializeTray();
        SourceInitialized += (_, _) => RestoreWindowPlacement();
        ContentRendered += async (_, _) => await InitializeConsumerStartupAsync();
        StateChanged += OnWindowStateChanged;
        Closing += OnWindowClosing;
    }

    private void LoadSettings()
    {
        loading = true;
        Appearance.SelectedIndex = (int)settings.Appearance;
        Voice.SelectedItem = Voices.OptionFor(settings.Voice);
        Speed.Value = settings.Speed;
        Volume.Value = settings.Volume;
        ReadNames.IsChecked = settings.ReadUsernames;
        IgnoreCommands.IsChecked = settings.IgnoreCommands;
        IgnoreUrls.IsChecked = settings.IgnoreUrls;
        IgnoreEmoji.IsChecked = settings.IgnoreEmoji;
        StartWithWindows.IsChecked = startupService.Read().Enabled;
        StartMinimized.IsChecked = settings.StartMinimizedToTray;
        AutomaticUpdates.IsChecked = settings.AutomaticallyCheckForUpdates;
        IgnoredUsers.Text = string.Join(", ", settings.IgnoredUsers);
        MaxQueue.Text = settings.MaxPending.ToString();
        MaxAge.Text = settings.MaxAgeSeconds.ToString();
        SpeechGap.Text = settings.SpeechGapMilliseconds.ToString();
        MaxLength.Text = settings.MaxMessageLength.ToString();
        loading = false;
        ApplySettings();
    }

    private void AppearanceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (loading) return;
        settings.Appearance = Appearance.SelectedIndex is >= 0 and <= 2 ? (AppearanceMode)Appearance.SelectedIndex : AppearanceMode.Dark;
        App.SetTheme(settings.Appearance);
        SaveSettings();
    }

    private void SettingsChanged(object sender, RoutedEventArgs e) { if (!loading) { ApplySettings(); SaveSettings(); } }
    private void SettingsChanged(object sender, SelectionChangedEventArgs e) { if (!loading) { ApplySettings(); SaveSettings(); } }

    private void ApplySettings()
    {
        settings.Voice = Voices.Resolve(Voice.SelectedItem);
        settings.Speed = Speed.Value;
        settings.Volume = Volume.Value;
        settings.ReadUsernames = ReadNames.IsChecked == true;
        settings.IgnoreCommands = IgnoreCommands.IsChecked == true;
        settings.IgnoreUrls = IgnoreUrls.IsChecked == true;
        settings.IgnoreEmoji = IgnoreEmoji.IsChecked != false;
        settings.StartMinimizedToTray = StartMinimized.IsChecked == true;
        settings.AutomaticallyCheckForUpdates = AutomaticUpdates.IsChecked != false;
        settings.IgnoredUsers = IgnoredUsers.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        settings.MaxPending = ReadInt(MaxQueue.Text, 6);
        settings.MaxAgeSeconds = ReadInt(MaxAge.Text, 30);
        settings.SpeechGapMilliseconds = ReadInt(SpeechGap.Text, 500);
        settings.MaxMessageLength = ReadInt(MaxLength.Text, 200);
        settings.Normalize();
        queue.Max = settings.MaxPending;
        queue.Age = TimeSpan.FromSeconds(settings.MaxAgeSeconds);
        filter.IgnoreCommands = settings.IgnoreCommands;
        filter.IgnoreUrls = settings.IgnoreUrls;
        filter.SetIgnoredUsers(settings.IgnoredUsers);
        pipeline.ReadUsernames = settings.ReadUsernames;
        pipeline.IgnoreEmoji = settings.IgnoreEmoji;
        pipeline.MaxMessageLength = settings.MaxMessageLength;
        SpeedValue.Text = $"{settings.Speed:0.0}×";
        VolumeValue.Text = $"{settings.Volume:P0}";
    }

    private void SaveSettings() => settingsStore.Save(settings);
    private static int ReadInt(string input, int fallback) => int.TryParse(input, out var value) ? value : fallback;
    private void Test(object sender, RoutedEventArgs e) { queue.Add(TestText.Text); AppendDiagnostic($"enqueue received depth={queue.Count}"); UpdateStatus(); }
    private void TogglePause(object sender, RoutedEventArgs e) => SetPaused(!paused);
    private void Clear(object sender, RoutedEventArgs e) { queue.Clear(); UpdateStatus(); }
    private void Stop(object sender, RoutedEventArgs e) { kokoro.Stop(); AppendDiagnostic("Stop Speaking requested"); UpdateStatus(); }
    private void SetPaused(bool value) { paused = value; PauseButton.Content = paused ? "Resume" : "Pause"; if (trayPauseItem is not null) trayPauseItem.Text = paused ? "Resume TTS" : "Pause TTS"; UpdateStatus(); }
    private void CopyDiagnostics(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(DiagnosticsText.Text ?? string.Empty); CopyDiagnosticsButton.Content = "Copied"; }
        catch { CopyDiagnosticsButton.Content = "Copy failed"; }
    }

    private async Task InitializeConsumerStartupAsync()
    {
        if (startHidden) HideToTray();
        if (settings.AutomaticallyCheckForUpdates) _ = CheckForUpdatesCoreAsync(true);
        await ConnectCoreAsync(false);
        if (health.State == TwitchState.NetworkError) _ = RetryStartupRestoreAsync();
    }

    private async Task RetryStartupRestoreAsync()
    {
        while (!appStopping.IsCancellationRequested && health.State == TwitchState.NetworkError && startupRestoreAttempt < 3)
        {
            var delay = RetryPolicy.Delay(startupRestoreAttempt++);
            await Task.Delay(delay, appStopping.Token);
            await ConnectCoreAsync(false);
        }
    }

    private async void Connect(object sender, RoutedEventArgs e) => await ConnectCoreAsync(true);

    private async Task ConnectCoreAsync(bool allowInteractiveAuthorization)
    {
        if (!await twitchGate.WaitAsync(0)) return;
        ConnectButton.IsEnabled = false;
        try
        {
            await StopTwitchAsync();
            SetTwitchState(TwitchState.Connecting);
            var tokens = new TokenLifecycle(http);
            var store = new DpapiAuthStore(diagnostic: message => Log("AUTH", message));
            var session = new TwitchSession(new DeviceCodeClient(http), tokens, store);
            var restored = await session.TryRestoreAsync(TwitchAppConfiguration.ClientId, appStopping.Token);
            TokenResponse auth;
            TokenValidation identity;
            if (restored.Kind == SessionRestoreKind.Valid && restored.Auth is not null && restored.Identity is not null)
            {
                auth = restored.Auth;
                identity = restored.Identity;
                Log("AUTH", "saved authorization validation succeeded");
            }
            else if (restored.Kind == SessionRestoreKind.TransientFailure)
            {
                SetTwitchState(TwitchState.NetworkError, restored.SafeDetail);
                Log("AUTH", "saved authorization validation transient failure: " + restored.SafeDetail);
                return;
            }
            else
            {
                if (restored.Kind == SessionRestoreKind.PermanentAuthorizationFailure) Log("AUTH", "saved authorization permanently rejected");
                if (!allowInteractiveAuthorization)
                {
                    SetTwitchState(TwitchState.AuthorizationRequired);
                    return;
                }
                SetTwitchState(TwitchState.Connecting);
                var authorized = await session.AuthorizeAsync(TwitchAppConfiguration.ClientId, _ => Dispatcher.Invoke(() => SetTwitchState(TwitchState.Connecting, "Waiting for authorization in browser")), appStopping.Token);
                auth = authorized.Auth;
                identity = authorized.Identity;
                Log("AUTH", "new authorization validated and saved");
            }
            await StartAuthenticatedAsync(tokens, store, auth, identity);
            ConnectButton.Content = "Connected";
            ConnectButton.IsEnabled = false;
        }
        catch (OperationCanceledException) when (appStopping.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Log("AUTH", "connect failure " + ex.GetType().Name);
            SetTwitchState(TwitchState.TwitchError, "connect failure: " + ex.GetType().Name);
        }
        finally { if (health.State != TwitchState.Connected) ConnectButton.IsEnabled = true; twitchGate.Release(); }
    }

    private void InitializeTray()
    {
        if (trayIcon is not null) return;
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Open ChatVox", null, (_, _) => RestoreFromTray());
        trayPauseItem = new Forms.ToolStripMenuItem("Pause TTS", null, (_, _) => Dispatcher.Invoke(() => SetPaused(!paused)));
        menu.Items.Add(trayPauseItem);
        menu.Items.Add("Exit ChatVox", null, async (_, _) => await Dispatcher.InvokeAsync(RequestExitAsync));
        trayIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "ChatVox.exe")),
            Text = "ChatVox — Live Chat TTS Reader",
            ContextMenuStrip = menu,
            Visible = true
        };
        trayIcon.DoubleClick += (_, _) => RestoreFromTray();
        Log("TRAY", "initialized");
    }

    private void HideToTray()
    {
        if (allowExit || !IsLoaded) return;
        CaptureWindowPlacement();
        Hide(); ShowInTaskbar = false;
        Log("TRAY", "hidden");
    }

    private void RestoreFromTray()
    {
        Dispatcher.Invoke(() =>
        {
            Show(); ShowInTaskbar = true; WindowState = lastVisibleWindowState; Activate(); Topmost = true; Topmost = false; Focus();
            Log("TRAY", "restored");
        });
    }

    public void RestoreFromSecondaryLaunch() => RestoreFromTray();

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (!allowExit)
        {
            e.Cancel = true;
            CaptureWindowPlacement();
            HideToTray();
        }
    }

    public void PrepareForSystemShutdown()
    {
        allowExit = true;
        _ = CleanShutdownAsync();
    }

    private async Task RequestExitAsync()
    {
        if (cleanupStarted) return;
        allowExit = true;
        await CleanShutdownAsync();
        trayIcon?.Dispose(); trayIcon = null;
        Close();
    }

    private async Task CleanShutdownAsync()
    {
        if (cleanupStarted) return;
        cleanupStarted = true;
        appStopping.Cancel();
        await StopTwitchAsync();
        CaptureWindowPlacement();
        settingsStore.Save(settings);
        await worker.DisposeAsync();
        kokoro.Dispose();
        http.Dispose();
        Log("APPLICATION", "shutdown");
    }

    private void StartupWithWindowsChanged(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        try
        {
            startupService.SetEnabled(StartWithWindows.IsChecked == true);
            var state = startupService.Read();
            StartWithWindows.IsChecked = state.Enabled;
            Log("STARTUP", state.Enabled ? "startup enabled" : "startup disabled");
        }
        catch (Exception ex)
        {
            StartWithWindows.IsChecked = startupService.Read().Enabled;
            AppendDiagnostic("Windows startup setting could not be changed: " + ex.GetType().Name);
            Log("STARTUP", "startup change failed " + ex.GetType().Name);
        }
    }

    private void StartMinimizedChanged(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        settings.StartMinimizedWasExplicitlySet = true;
        ApplySettings();
        SaveSettings();
    }

    private async void CheckForUpdates(object sender, RoutedEventArgs e) => await CheckForUpdatesCoreAsync(false);
    private async Task CheckForUpdatesCoreAsync(bool automatic)
    {
        try
        {
            UpdateStatusText.Text = "Checking for updates...";
            var result = await new UpdateService().CheckAsync(version, appStopping.Token);
            settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow; SaveSettings();
            if (result.IsConfigured) settings.LastSuccessfulUpdateCheckUtc = DateTimeOffset.UtcNow;
            if (result.IsUpdateAvailable) settings.LatestKnownEligibleRelease = result.AvailableVersion;
            SaveSettings();
            pendingUpdate = result.IsUpdateAvailable ? result : null;
            UpdateActionButton.Visibility = pendingUpdate is null ? Visibility.Collapsed : Visibility.Visible;
            UpdateStatusText.Text = result.SafeMessage;
            Log("UPDATE", automatic ? "automatic update check " + (result.IsConfigured ? "completed" : "not configured") : "manual update check " + (result.IsConfigured ? "completed" : "not configured"));
        }
        catch (OperationCanceledException) when (appStopping.IsCancellationRequested) { }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "Unable to check for updates right now.";
            Log("UPDATE", "update check failed " + ex.GetType().Name);
        }
    }

    private async void DownloadAndInstallUpdate(object sender, RoutedEventArgs e)
    {
        if (pendingUpdate is null) return;
        try
        {
            UpdateActionButton.IsEnabled = false;
            UpdateStatusText.Text = "Downloading update...";
            var verified = await new UpdateService().DownloadAndVerifyAsync(pendingUpdate, new Progress<int>(value => UpdateStatusText.Text = $"Downloading update... {value}%"), appStopping.Token);
            UpdateStatusText.Text = "Verified update. Starting installer...";
            Log("UPDATE", "verified installer ready " + verified.Version);
            Process.Start(new ProcessStartInfo(verified.InstallerPath) { UseShellExecute = true });
            await RequestExitAsync();
        }
        catch (OperationCanceledException) when (appStopping.IsCancellationRequested) { }
        catch (Exception ex)
        {
            UpdateStatusText.Text = "Verification failed or update download could not complete.";
            Log("UPDATE", "download or verification failed " + ex.GetType().Name);
            UpdateActionButton.IsEnabled = true;
        }
    }

    private async Task StartAuthenticatedAsync(TokenLifecycle tokens, DpapiAuthStore store, TokenResponse auth, TokenValidation identity)
    {
        activeAuth = auth;
        activeIdentity = identity;
        twitchRun = CancellationTokenSource.CreateLinkedTokenSource(appStopping.Token);
        await StartEventSubAsync(twitchRun.Token);
        tokenMonitorTask = new TokenMonitor(tokens).RunAsync(
            () => activeAuth ?? throw new InvalidOperationException("Missing active Twitch authorization."),
            refreshed => HandleRefreshSuccessAsync(store, refreshed),
            result => HandleValidationObservedAsync(result),
            result => HandleRefreshObservedAsync(store, result),
            twitchRun.Token);
    }

    private async Task StartEventSubAsync(CancellationToken ct)
    {
        if (activeAuth is null || activeIdentity is null) return;
        var previous = eventSub;
        if (previous is not null) await previous.DisposeAsync();
        eventSub = new EventSubClient(http, message => Log("EVENTSUB", message));
        eventSub.Diagnostic += AppendDiagnostic;
        eventSub.Status += OnEventSubStatus;
        eventSub.Chat += chat =>
        {
            if (pipeline.Accept(chat)) { AppendDiagnostic($"enqueue received depth={queue.Count}"); Dispatcher.BeginInvoke(UpdateStatus); }
            else AppendDiagnostic("EventSub notification rejected by filter or dedup");
        };
        await eventSub.ConnectAsync(activeAuth.AccessToken, TwitchAppConfiguration.ClientId, activeIdentity.UserId, ct);
    }

    private async Task HandleValidationObservedAsync(TokenValidationResult result)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            health.Validation(result);
            Log("AUTH", "validation result " + result.Kind + (result.HttpStatus is null ? string.Empty : " HTTP " + result.HttpStatus));
            if (result.Kind == TokenValidationKind.TransientFailure) SetTwitchState(TwitchState.NetworkError, result.SafeDetail);
            else if (result.Kind == TokenValidationKind.Success && health.State == TwitchState.NetworkError && eventSub is not null) SetTwitchState(TwitchState.Connected);
        });
    }

    private async Task HandleRefreshObservedAsync(DpapiAuthStore store, TokenRefreshResult result)
    {
        await Dispatcher.InvokeAsync(() => Log("AUTH", "refresh result " + result.Kind + (result.HttpStatus is null ? string.Empty : " HTTP " + result.HttpStatus)));
        if (result.Kind == TokenRefreshKind.TransientFailure)
        {
            await Dispatcher.InvokeAsync(() => SetTwitchState(TwitchState.NetworkError, result.SafeDetail));
            return;
        }
        if (result.Kind == TokenRefreshKind.PermanentAuthFailure)
        {
            store.Clear();
            twitchRun?.Cancel();
            var previous = eventSub;
            eventSub = null;
            if (previous is not null) await previous.DisposeAsync();
            await Dispatcher.InvokeAsync(() => SetTwitchState(TwitchState.AuthorizationRequired, "refresh permanently rejected"));
        }
    }

    private async Task HandleRefreshSuccessAsync(DpapiAuthStore store, TokenResponse refreshed)
    {
        activeAuth = refreshed;
        var saved = store.TrySave(refreshed);
        Log("AUTH", saved ? "refresh succeeded and persisted" : "refresh succeeded but auth persistence failed");
        await Dispatcher.InvokeAsync(() => SetTwitchState(saved ? TwitchState.Reconnecting : TwitchState.TwitchError, saved ? "token refreshed" : "auth blob write failed"));
        if (twitchRun is not null) await StartEventSubAsync(twitchRun.Token);
    }

    private void OnEventSubStatus(string status)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var state = status switch
            {
                "Connected" => TwitchState.Connected,
                "Connecting" => TwitchState.Connecting,
                "Reconnecting" => TwitchState.Reconnecting,
                _ => TwitchState.TwitchError
            };
            SetTwitchState(state, state == TwitchState.TwitchError ? status : null);
        });
    }

    private async Task StopTwitchAsync()
    {
        twitchRun?.Cancel();
        var previous = eventSub;
        eventSub = null;
        if (previous is not null) await previous.DisposeAsync();
        if (tokenMonitorTask is not null) try { await tokenMonitorTask; } catch (OperationCanceledException) { }
        tokenMonitorTask = null;
        twitchRun?.Dispose();
        twitchRun = null;
        activeAuth = null;
        activeIdentity = null;
    }

    private void SetTwitchState(TwitchState state, string? safeDetail = null)
    {
        health.SetState(state, safeDetail);
        TwitchStatus.Text = "Twitch  " + (state switch
        {
            TwitchState.AuthorizationRequired => "Authorization Required",
            TwitchState.NetworkError => "Network Error",
            TwitchState.TwitchError => "Twitch Error",
            _ => state.ToString()
        });
        TwitchDot.Fill = Brush(state switch
        {
            TwitchState.Connected => "#53C47A",
            TwitchState.AuthorizationRequired or TwitchState.TwitchError => "#E75A5A",
            _ => "#F0A83A"
        });
        if (state == TwitchState.Connected)
        {
            ConnectButton.Content = "Connected";
            ConnectButton.IsEnabled = false;
        }
        else if (state == TwitchState.AuthorizationRequired)
        {
            ConnectButton.Content = "Connect Twitch";
            ConnectButton.IsEnabled = true;
        }
        if (!string.IsNullOrWhiteSpace(safeDetail)) AppendDiagnostic(safeDetail);
        else RefreshDiagnostics();
    }

    private void UpdateStatus()
    {
        TtsStatus.Text = paused ? "TTS  Paused" : worker.IsSpeaking ? "TTS  Speaking" : "TTS  Ready";
        TtsDot.Fill = Brush(paused ? "#F0A83A" : "#53C47A");
        QueueStatus.Text = $"Queue  {queue.Count} / {queue.Max}";
        QueueDot.Fill = Brush(queue.Count >= queue.Max ? "#F0A83A" : "#53C47A");
        RefreshDiagnostics();
    }

    private void Log(string area, string message) => log.Write(area, message);
    private void AppendDiagnostic(string line)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(() => AppendDiagnostic(line)); return; }
        while (diagnosticLines.Count >= 8) diagnosticLines.Dequeue();
        diagnosticLines.Enqueue(line);
        RefreshDiagnostics();
    }
    private static string FormatTime(DateTimeOffset? time) => time?.ToString("yyyy-MM-dd HH:mm:ss 'UTC'") ?? "not yet";
    private void RefreshDiagnostics() => DiagnosticsText.Text = $"Version: {version}\nTwitch: {health.State switch { TwitchState.AuthorizationRequired => "Authorization Required", TwitchState.NetworkError => "Network Error", TwitchState.TwitchError => "Twitch Error", _ => health.State.ToString() }}\nTTS: {(paused ? "Paused" : worker.IsSpeaking ? "Speaking" : "Ready")}\nQueue: {queue.Count} / {queue.Max}  •  max age {settings.MaxAgeSeconds}s  •  gap {settings.SpeechGapMilliseconds}ms\nValidation: {health.LastValidationResult}  •  last success {FormatTime(health.LastSuccessfulValidation)}\nEventSub: last connected {FormatTime(health.LastSuccessfulEventSubConnection)}  •  reconnect attempt {health.ReconnectAttempt}\nStartup with Windows: {(startupService.Read().Enabled ? "Enabled" : "Disabled")}  •  Start minimized: {(settings.StartMinimizedToTray ? "Enabled" : "Disabled")}  •  Tray: {(IsVisible ? "Visible" : "Hidden")}\nLast Twitch error: {health.LastSafeError ?? "none"}\n\n{string.Join(Environment.NewLine, diagnosticLines)}";
    private static System.Windows.Media.Brush Brush(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        if (WindowState is WindowState.Normal or WindowState.Maximized)
            lastVisibleWindowState = WindowState;
        if (WindowState == WindowState.Minimized)
            HideToTray();
    }

    private void RestoreWindowPlacement()
    {
        var areas = Forms.Screen.AllScreens.Select(screen => ToLogicalWorkArea(screen.WorkingArea)).ToArray();
        var saved = settings.WindowLeft is { } left && settings.WindowTop is { } top && settings.WindowWidth is { } width && settings.WindowHeight is { } height
            ? new WindowBounds(left, top, width, height)
            : (WindowBounds?)null;
        var placement = WindowGeometry.Restore(saved, areas);
        Left = placement.Left;
        Top = placement.Top;
        Width = placement.Width;
        Height = placement.Height;
        lastVisibleWindowState = settings.WindowWasMaximized ? WindowState.Maximized : WindowState.Normal;
        if (settings.WindowWasMaximized)
            Dispatcher.BeginInvoke(() => WindowState = WindowState.Maximized);
    }

    private WorkArea ToLogicalWorkArea(System.Drawing.Rectangle physical)
    {
        var dpi = VisualTreeHelper.GetDpi(this);
        return new WorkArea(physical.Left / dpi.DpiScaleX, physical.Top / dpi.DpiScaleY, physical.Width / dpi.DpiScaleX, physical.Height / dpi.DpiScaleY);
    }

    private void CaptureWindowPlacement()
    {
        if (!IsLoaded) return;
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, Width, Height)
            : RestoreBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        settings.WindowLeft = bounds.Left;
        settings.WindowTop = bounds.Top;
        settings.WindowWidth = bounds.Width;
        settings.WindowHeight = bounds.Height;
        settings.WindowWasMaximized = lastVisibleWindowState == WindowState.Maximized;
    }
}
