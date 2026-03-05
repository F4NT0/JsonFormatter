using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using JsonFormatter.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
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
        SearchBox.TextChanged += SearchBox_TextChanged;
        SyncStatus();
    }

    private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        UpdateHighlightedPreview();
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
            LineNumbersText.Text = string.Empty;
            return;
        }

        var json = _vm.JsonText;
        HighlightedText.Inlines.Clear();

        var lineCount = json.Split('\n').Length;
        LineNumbersText.Text = string.Join("\n", Enumerable.Range(1, lineCount));

        var searchTerm = SearchBox?.Text ?? string.Empty;
        bool hasSearch = !string.IsNullOrEmpty(searchTerm);
        bool searchFound = false;

        // Build list of syntax-colored spans for the whole JSON
        var pattern = @"(""(?:[^""\\]|\\.)*"")|(-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)|(true|false|null)|([{}\[\],:])";
        var regex = new Regex(pattern);

        // Collect segments: (start, length, syntaxColor)
        var segments = new List<(int start, int length, string color)>();
        int lastPos = 0;
        foreach (Match match in regex.Matches(json))
        {
            if (match.Index > lastPos)
                segments.Add((lastPos, match.Index - lastPos, "#CDD6F4"));

            var value = match.Value;
            string color;
            if (value.StartsWith("\"") && value.EndsWith("\""))
                color = "#F1FA8C";
            else if (Regex.IsMatch(value, @"^-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?$"))
                color = "#BD93F9";
            else if (value is "true" or "false" or "null")
                color = "#FF5555";
            else if (value is "{" or "}" or "[" or "]")
                color = "#8BE9FD";
            else
                color = "#FFFFFF";

            segments.Add((match.Index, match.Length, color));
            lastPos = match.Index + match.Length;
        }
        if (lastPos < json.Length)
            segments.Add((lastPos, json.Length - lastPos, "#CDD6F4"));

        // Render segments, splitting by search term when present
        foreach (var (start, length, color) in segments)
        {
            var text = json.Substring(start, length);
            if (hasSearch)
            {
                int idx = 0;
                while (idx < text.Length)
                {
                    int found = text.IndexOf(searchTerm, idx, StringComparison.OrdinalIgnoreCase);
                    if (found < 0)
                    {
                        HighlightedText.Inlines.Add(new Run { Text = text.Substring(idx), Foreground = new SolidColorBrush(Color.Parse(color)) });
                        break;
                    }
                    searchFound = true;
                    if (found > idx)
                        HighlightedText.Inlines.Add(new Run { Text = text.Substring(idx, found - idx), Foreground = new SolidColorBrush(Color.Parse(color)) });
                    // Search match: orange background simulation via bold orange foreground
                    HighlightedText.Inlines.Add(new Run
                    {
                        Text = text.Substring(found, searchTerm.Length),
                        Foreground = new SolidColorBrush(Color.Parse("#FF9900")),
                        FontWeight = Avalonia.Media.FontWeight.Bold
                    });
                    idx = found + searchTerm.Length;
                }
            }
            else
            {
                HighlightedText.Inlines.Add(new Run { Text = text, Foreground = new SolidColorBrush(Color.Parse(color)) });
            }
        }

        // Update search status icon
        if (hasSearch)
        {
            SearchStatusIcon.IsVisible = true;
            SearchStatusIcon.Text = searchFound ? "\uF05D" : "\uF52F";
            SearchStatusIcon.Foreground = new SolidColorBrush(Color.Parse(searchFound ? "#50FA7B" : "#FF5555"));
        }
        else
        {
            SearchStatusIcon.IsVisible = false;
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
        EditBtn.IsVisible = true;
        UpdateHighlightedPreview();
        
        _ = _vm.ShowNotificationAsync("JSON beautified!", true);
    }

    private void EditBtn_Click(object? sender, RoutedEventArgs e)
    {
        _isBeautified = false;
        Editor.IsVisible = true;
        HighlightedViewer.IsVisible = false;
        EditBtn.IsVisible = false;
        SearchStatusIcon.IsVisible = false;
        Editor.Focus();
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
        EditBtn.IsVisible = false;
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
