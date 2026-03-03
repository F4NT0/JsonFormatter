using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using JsonFormatter.ViewModels;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonFormatter;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel _vm;
    private bool _updatingFromVm = false;
    private bool _isBeautified = false;

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
        
        // When user edits, switch back to TextBox mode
        if (_isBeautified)
        {
            _isBeautified = false;
            Editor.IsVisible = true;
            HighlightedViewer.IsVisible = false;
        }
    }

    private void SyncStatus()
    {
        bool valid     = _vm.IsJsonValid;
        bool formatted = _vm.IsJsonFormatted;
        string errMsg  = _vm.ValidationMessage ?? "";

        ValidDotText.Text           = valid ? "\uF05D" : "\uF52F";
        ValidDotText.Foreground     = new SolidColorBrush(Color.Parse(valid ? "#50FA7B" : "#FF5555"));
        ValidLabel.Foreground       = new SolidColorBrush(Color.Parse("#CDD6F4"));

        FormattedDotText.Text       = formatted ? "\uF05D" : "\uF52F";
        FormattedDotText.Foreground = new SolidColorBrush(Color.Parse(formatted ? "#50FA7B" : "#FF5555"));
        FormattedLabel.Foreground   = new SolidColorBrush(Color.Parse("#CDD6F4"));

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
                    var caret = Editor.CaretIndex;
                    Editor.Text = _vm.JsonText;
                    Editor.CaretIndex = Math.Min(caret, Editor.Text?.Length ?? 0);
                    _updatingFromVm = false;
                }
                UpdateHighlightedPreview();
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

    private void UpdateHighlightedPreview()
    {
        if (!_vm.IsJsonValid || string.IsNullOrWhiteSpace(_vm.JsonText))
        {
            HighlightedText.Inlines.Clear();
            return;
        }

        var json = _vm.JsonText;
        HighlightedText.Inlines.Clear();
        
        // Simple regex-based highlighting
        var pattern = @"(""(?:[^""\\]|\\.)*"")|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)|(true|false|null)|([{}\[\],:])";
        var regex = new Regex(pattern);
        
        int lastPos = 0;
        foreach (Match match in regex.Matches(json))
        {
            // Add text before this match (default color)
            if (match.Index > lastPos)
            {
                var plainText = json.Substring(lastPos, match.Index - lastPos);
                HighlightedText.Inlines.Add(new Run { Text = plainText, Foreground = new SolidColorBrush(Color.Parse("#CDD6F4")) });
            }
            
            // Add highlighted match
            var value = match.Value;
            SolidColorBrush brush;
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                // String - yellow
                brush = new SolidColorBrush(Color.Parse("#F1FA8C"));
            }
            else if (Regex.IsMatch(value, @"^-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?$"))
            {
                // Number - purple
                brush = new SolidColorBrush(Color.Parse("#BD93F9"));
            }
            else if (value is "true" or "false" or "null")
            {
                // Boolean/null - red
                brush = new SolidColorBrush(Color.Parse("#FF5555"));
            }
            else if (value is "{" or "}" or "[" or "]")
            {
                // Brackets - light blue
                brush = new SolidColorBrush(Color.Parse("#8BE9FD"));
            }
            else // comma or colon
            {
                // Comma/colon - white
                brush = new SolidColorBrush(Color.Parse("#FFFFFF"));
            }
            
            HighlightedText.Inlines.Add(new Run { Text = value, Foreground = brush });
            lastPos = match.Index + match.Length;
        }
        
        // Add remaining text
        if (lastPos < json.Length)
        {
            var plainText = json.Substring(lastPos);
            HighlightedText.Inlines.Add(new Run { Text = plainText, Foreground = new SolidColorBrush(Color.Parse("#CDD6F4")) });
        }
    }

    private void SyncNotification()
    {
        bool show    = _vm.ShowNotification;
        bool success = _vm.NotificationIsSuccess;

        NotificationBorder.IsVisible   = show;
        NotificationBorder.Background  = new SolidColorBrush(Color.Parse(success ? "#1E3A2D" : "#3A1E1E"));
        NotificationBorder.BorderBrush = new SolidColorBrush(Color.Parse(success ? "#50FA7B" : "#FF5555"));
        NotifIcon.Text      = success ? "\uF05D" : "\uF52F";
        NotifIcon.Foreground = new SolidColorBrush(Color.Parse(success ? "#50FA7B" : "#FF5555"));
        NotifText.Text      = _vm.NotificationMessage ?? "";
    }

    private void BeautifyBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_vm.JsonText))
        {
            _ = _vm.ShowNotificationAsync("No JSON to beautify.", false);
            return;
        }
        _vm.BeautifyJson();
        _updatingFromVm = true;
        Editor.Text = _vm.JsonText;
        _updatingFromVm = false;
        SyncStatus();
        
        // Switch to highlighted view
        _isBeautified = true;
        Editor.IsVisible = false;
        HighlightedViewer.IsVisible = true;
        UpdateHighlightedPreview();
        
        _ = _vm.ShowNotificationAsync("JSON beautified!", true);
    }

    private void ClearBtn_Click(object? sender, RoutedEventArgs e)
    {
        _updatingFromVm = true;
        Editor.Text = string.Empty;
        _updatingFromVm = false;
        _vm.JsonText = string.Empty;
        SyncStatus();
        
        // Reset to TextBox mode
        _isBeautified = false;
        Editor.IsVisible = true;
        HighlightedViewer.IsVisible = false;
        HighlightedText.Text = "";
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
                    
                    // If JSON is already formatted, show highlighted view
                    if (_vm.IsJsonFormatted)
                    {
                        _isBeautified = true;
                        Editor.IsVisible = false;
                        HighlightedViewer.IsVisible = true;
                        UpdateHighlightedPreview();
                    }
                    else
                    {
                        _isBeautified = false;
                        Editor.IsVisible = true;
                        HighlightedViewer.IsVisible = false;
                    }
                    
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
