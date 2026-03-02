using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using JsonFormatter.ViewModels;
using System;
using System.Threading.Tasks;

namespace JsonFormatter;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;
    private bool _updatingFromVm = false;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainWindowViewModel();
        DataContext = _vm;
        _vm.PropertyChanged += Vm_PropertyChanged;
        this.Loaded += OnWindowLoaded;
    }

    private void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        Editor.TextChanged += Editor_TextChanged;
        SyncStatus();
    }

    private void Editor_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_updatingFromVm) return;
        _vm.JsonText = Editor.Text ?? string.Empty;
        SyncStatus();
    }

    private void SyncStatus()
    {
        bool valid     = _vm.IsJsonValid;
        bool formatted = _vm.IsJsonFormatted;
        string errMsg  = _vm.ValidationMessage ?? "";

        ValidDot.Background         = new SolidColorBrush(Color.Parse(valid ? "#2D4A3E" : "#2D1B1B"));
        ValidDotText.Text           = valid ? "✓" : "✗";
        ValidDotText.Foreground     = new SolidColorBrush(Color.Parse(valid ? "#50FA7B" : "#FF5555"));
        ValidLabel.Foreground       = new SolidColorBrush(Color.Parse(valid ? "#50FA7B" : "#6272A4"));

        FormattedDot.Background     = new SolidColorBrush(Color.Parse(formatted ? "#3A2D4A" : "#2D1B1B"));
        FormattedDotText.Text       = formatted ? "✓" : "✗";
        FormattedDotText.Foreground = new SolidColorBrush(Color.Parse(formatted ? "#BD93F9" : "#FF5555"));
        FormattedLabel.Foreground   = new SolidColorBrush(Color.Parse(formatted ? "#BD93F9" : "#6272A4"));

        ErrorBar.IsVisible  = !valid && !string.IsNullOrEmpty(errMsg);
        ErrorText.Text      = errMsg;
        OneLineText.Text    = _vm.OneLineJson ?? "";
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.JsonText))
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (Editor.Text != _vm.JsonText)
                {
                    _updatingFromVm = true;
                    Editor.Text = _vm.JsonText;
                    _updatingFromVm = false;
                }
                SyncStatus();
            });
        }

        if (e.PropertyName is nameof(MainWindowViewModel.IsJsonValid)
                           or nameof(MainWindowViewModel.IsJsonFormatted)
                           or nameof(MainWindowViewModel.ValidationMessage)
                           or nameof(MainWindowViewModel.OneLineJson))
        {
            Dispatcher.UIThread.InvokeAsync(SyncStatus);
        }

        if (e.PropertyName is nameof(MainWindowViewModel.ShowNotification)
                           or nameof(MainWindowViewModel.NotificationIsSuccess)
                           or nameof(MainWindowViewModel.NotificationMessage))
        {
            Dispatcher.UIThread.InvokeAsync(SyncNotification);
        }
    }

    private void SyncNotification()
    {
        bool show    = _vm.ShowNotification;
        bool success = _vm.NotificationIsSuccess;

        NotificationBorder.IsVisible   = show;
        NotificationBorder.Background  = new SolidColorBrush(Color.Parse(success ? "#1E3A2D" : "#3A1E1E"));
        NotificationBorder.BorderBrush = new SolidColorBrush(Color.Parse(success ? "#50FA7B" : "#FF5555"));
        NotifIcon.Text      = success ? "✓" : "✗";
        NotifIcon.Foreground = new SolidColorBrush(Color.Parse(success ? "#50FA7B" : "#FF5555"));
        NotifText.Text      = _vm.NotificationMessage ?? "";
    }

    private void BeautifyBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (!_vm.IsJsonValid)
        {
            _ = _vm.ShowNotificationAsync("Fix JSON errors before beautifying.", false);
            return;
        }
        _vm.BeautifyJson();
        _updatingFromVm = true;
        Editor.Text = _vm.JsonText;
        _updatingFromVm = false;
        SyncStatus();
        _ = _vm.ShowNotificationAsync("JSON beautified!", true);
    }

    private void ClearBtn_Click(object? sender, RoutedEventArgs e)
    {
        _updatingFromVm = true;
        Editor.Text = string.Empty;
        _updatingFromVm = false;
        _vm.JsonText = string.Empty;
        SyncStatus();
    }

    private async void CopyBtn_Click(object? sender, RoutedEventArgs e)
        => await CopyToClipboard(_vm.JsonText, "JSON copied!", "Copy failed.");

    private async void CopyOneLineBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_vm.OneLineJson))
        {
            _ = _vm.ShowNotificationAsync("No valid JSON to copy.", false);
            return;
        }
        await CopyToClipboard(_vm.OneLineJson, "One-line JSON copied!", "Copy failed.");
    }

    private async Task CopyToClipboard(string text, string ok, string fail)
    {
        try
        {
            var cb = TopLevel.GetTopLevel(this)?.Clipboard;
            if (cb != null) { await cb.SetTextAsync(text); _ = _vm.ShowNotificationAsync(ok, true); }
            else _ = _vm.ShowNotificationAsync(fail, false);
        }
        catch { _ = _vm.ShowNotificationAsync(fail, false); }
    }

    private async void ImportBtn_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top == null) return;
            var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import JSON File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files")  { Patterns = new[] { "*.*" } }
                }
            });
            if (files.Count > 0)
            {
                var result = _vm.ImportJson(files[0].Path.LocalPath);
                if (result == "success")
                {
                    _updatingFromVm = true;
                    Editor.Text = _vm.JsonText;
                    _updatingFromVm = false;
                    SyncStatus();
                    _ = _vm.ShowNotificationAsync($"Imported: {files[0].Name}", true);
                }
                else _ = _vm.ShowNotificationAsync($"Import failed: {result}", false);
            }
        }
        catch (Exception ex) { _ = _vm.ShowNotificationAsync($"Import failed: {ex.Message}", false); }
    }

    private async void ExportBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_vm.JsonText)) { _ = _vm.ShowNotificationAsync("Nothing to export.", false); return; }
        try
        {
            var top = TopLevel.GetTopLevel(this);
            if (top == null) return;
            var file = await top.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export JSON File",
                SuggestedFileName = "output.json",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON Files") { Patterns = new[] { "*.json" } },
                    new FilePickerFileType("All Files")  { Patterns = new[] { "*.*" } }
                }
            });
            if (file != null)
            {
                var result = _vm.ExportJson(file.Path.LocalPath);
                _ = result == "success"
                    ? _vm.ShowNotificationAsync($"Exported: {file.Name}", true)
                    : _vm.ShowNotificationAsync($"Export failed: {result}", false);
            }
        }
        catch (Exception ex) { _ = _vm.ShowNotificationAsync($"Export failed: {ex.Message}", false); }
    }
}
