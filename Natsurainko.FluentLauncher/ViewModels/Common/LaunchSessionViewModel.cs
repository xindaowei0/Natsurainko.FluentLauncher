﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Natsurainko.FluentLauncher.Classes.Data.UI;
using Natsurainko.FluentLauncher.Services.Launch;
using Nrk.FluentCore.Launch;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Windows.ApplicationModel;
using WinUIEx;

namespace Natsurainko.FluentLauncher.Components.Launch;
#nullable enable

internal partial class LaunchSessionViewModel : ObservableObject
{
    private readonly MinecraftSession _launchSession;
    private GameInfo _gameInfo => _launchSession.GameInfo;

    public LaunchSessionViewModel(MinecraftSession session) : base()
    {
        _launchSession = session;

        // Handles all state changes
        session.StateChanged += (_, e)
            => App.DispatcherQueue.TryEnqueue(() =>
            {
                DisplayState = e.NewState;
            });

        // Retrieve the process output streams
        session.OutputDataReceived += McProcess_OutputDataReceived;
        session.ErrorDataReceived += McProcess_ErrorDataReceived;

        // TODO: show exception when falted

        //State = MinecraftSessionState.Faulted;
        //_mcProcess?.Dispose();
        //App.DispatcherQueue.TryEnqueue(() =>
        //{
        //    Exception = ex;
        //    ExceptionReason = Exception.Message;
        //});
    }

    private void McProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            _gameLoggerOutputs.Add(GameLoggerOutput.Parse(e.Data, true));
    }

    private void McProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (!string.IsNullOrEmpty(e.Data))
            _gameLoggerOutputs.Add(GameLoggerOutput.Parse(e.Data));
    }

    public LaunchExpanderStepItem[] StepItems { get; } = new[]
    {
        new LaunchExpanderStepItem { Step = MinecraftSessionState.Inspecting },
        new LaunchExpanderStepItem { Step = MinecraftSessionState.Authenticating },
        new LaunchExpanderStepItem { Step = MinecraftSessionState.CompletingResources },
        new LaunchExpanderStepItem { Step = MinecraftSessionState.BuildingArguments },
        new LaunchExpanderStepItem { Step = MinecraftSessionState.LaunchingProcess }
    };

    private int lastState;

    [ObservableProperty]
    private bool isExpanded = true;

    [ObservableProperty]
    private MinecraftSessionState displayState;

    [ObservableProperty]
    private string? processStartTime;

    [ObservableProperty]
    private string? processExitTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private double progress;

    [ObservableProperty]
    private Exception? exception;

    [ObservableProperty]
    private string? exceptionReason;

    public ObservableCollection<GameLoggerOutput> _gameLoggerOutputs = new();

    public string ProgressText => Progress.ToString("P1");

    partial void OnDisplayStateChanged(MinecraftSessionState oldValue, MinecraftSessionState newValue)
    {
        var state = (int)DisplayState;

        if (2 <= state && state <= 6)
        {
            StepItems[lastState - 1].RunState = 2;
            if (state != 4) StepItems[lastState - 1].FinishedTaskNumber = 1;
        }

        if (1 <= state && state <= 5)
            StepItems[state - 1].RunState = 1;

        if (DisplayState == MinecraftSessionState.Faulted)
            StepItems[lastState - 1].RunState = -1;

        if (DisplayState == MinecraftSessionState.GameRunning)
            ProcessStartTime = $"[{DateTime.Now:HH:mm:ss}]";

        if (DisplayState == MinecraftSessionState.GameExited)
            IsExpanded = false;

        if (DisplayState == MinecraftSessionState.GameExited || DisplayState == MinecraftSessionState.GameCrashed)
            ProcessExitTime = $"[{DateTime.Now:HH:mm:ss}]";

        lastState = state;

        UpdateLaunchProgress();
    }

    internal void UpdateLaunchProgress()
    {
        int total = StepItems.Select(x => x.TaskNumber).Sum();
        int finished = StepItems.Select(x => x.FinishedTaskNumber).Sum();

        Progress = (double)finished / total;
    }

    internal void UpdateDownloadProgress()
    {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        Interlocked.Increment(ref StepItems[2].finishedTaskNumber);
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        StepItems[2].OnFinishedTaskNumberUpdate();

        UpdateLaunchProgress();
    }

    [RelayCommand]
    public void KillButton() => _launchSession.Kill();

    [RelayCommand]
    public void LoggerButton()
    {
        App.DispatcherQueue.TryEnqueue(() =>
        {
            var window = new WindowEx();

            window.Title = $"Logger - {_gameInfo.AbsoluteId}";

            window.AppWindow.SetIcon(Path.Combine(Package.Current.InstalledLocation.Path, "Assets/AppIcon.ico"));
            window.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            window.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            window.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            window.SystemBackdrop = Environment.OSVersion.Version.Build >= 22000
               ? new MicaBackdrop() { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt }
               : new DesktopAcrylicBackdrop();

            (window.MinWidth, window.MinHeight) = (516, 328);
            (window.Width, window.Height) = (873, 612);

            var view = new Views.LoggerPage();
            var viewModel = new ViewModels.Pages.LoggerViewModel(this, view);
            viewModel.Title = window.Title;
            view.DataContext = viewModel;

            window.Content = view;
            window.Show();
        });
    }
}
