using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace JsonFormatter.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private string _jsonText = string.Empty;
    private string _oneLineJson = string.Empty;
    private string _validationMessage = string.Empty;
    private bool _isJsonValid = false;
    private bool _isJsonFormatted = false;
    private string _notificationMessage = string.Empty;
    private bool _showNotification = false;
    private bool _notificationIsSuccess = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string JsonText
    {
        get => _jsonText;
        set
        {
            if (_jsonText != value)
            {
                _jsonText = value;
                OnPropertyChanged();
                ValidateJson();
            }
        }
    }

    public string OneLineJson
    {
        get => _oneLineJson;
        set { _oneLineJson = value; OnPropertyChanged(); }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set { _validationMessage = value; OnPropertyChanged(); }
    }

    public bool IsJsonValid
    {
        get => _isJsonValid;
        set { _isJsonValid = value; OnPropertyChanged(); }
    }

    public bool IsJsonFormatted
    {
        get => _isJsonFormatted;
        set { _isJsonFormatted = value; OnPropertyChanged(); }
    }

    public string NotificationMessage
    {
        get => _notificationMessage;
        set { _notificationMessage = value; OnPropertyChanged(); }
    }

    public bool ShowNotification
    {
        get => _showNotification;
        set { _showNotification = value; OnPropertyChanged(); }
    }

    public bool NotificationIsSuccess
    {
        get => _notificationIsSuccess;
        set { _notificationIsSuccess = value; OnPropertyChanged(); }
    }

    public void ValidateJson()
    {
        if (string.IsNullOrWhiteSpace(_jsonText))
        {
            ValidationMessage = string.Empty;
            IsJsonValid = false;
            IsJsonFormatted = false;
            OneLineJson = string.Empty;
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(_jsonText, new JsonDocumentOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Skip
            });

            IsJsonValid = true;
            ValidationMessage = string.Empty;

            var oneLine = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            OneLineJson = oneLine;

            var beautified = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            IsJsonFormatted = IsAlreadyBeautified(beautified, _jsonText);
        }
        catch (JsonException ex)
        {
            IsJsonValid = false;
            IsJsonFormatted = false;
            OneLineJson = string.Empty;

            var msg = ex.Message;
            if (ex.LineNumber.HasValue)
                msg = $"Line {ex.LineNumber + 1}, Position {ex.BytePositionInLine}: {GetFriendlyError(ex.Message)}";
            else
                msg = GetFriendlyError(ex.Message);

            ValidationMessage = msg;
        }
    }

    private bool IsAlreadyBeautified(string beautified, string current)
    {
        return string.Equals(
            beautified.Replace("\r\n", "\n").Trim(),
            current.Replace("\r\n", "\n").Trim(),
            StringComparison.Ordinal);
    }

    private string GetFriendlyError(string message)
    {
        if (message.Contains("'\"'") || message.Contains("missing"))
            return "Missing double quote detected. Check your string values.";
        if (message.Contains("trailing comma") || message.Contains("trailing"))
            return "Trailing comma found. Remove the last comma before closing bracket.";
        if (message.Contains("'}'" ) || message.Contains("']'"))
            return "Missing closing bracket or brace.";
        if (message.Contains("property name"))
            return "Invalid property name. Keys must be in double quotes.";
        if (message.Contains("value") && message.Contains("invalid"))
            return "Invalid value. Expected a string, number, boolean, null, array, or object.";
        return message;
    }

    public void BeautifyJson()
    {
        if (!IsJsonValid || string.IsNullOrWhiteSpace(_jsonText))
            return;

        try
        {
            using var doc = JsonDocument.Parse(_jsonText);
            var beautified = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            _jsonText = beautified;
            OnPropertyChanged(nameof(JsonText));
            IsJsonFormatted = true;
        }
        catch { }
    }

    public async Task ShowNotificationAsync(string message, bool success = true)
    {
        NotificationMessage = message;
        NotificationIsSuccess = success;
        ShowNotification = true;
        await Task.Delay(3000);
        ShowNotification = false;
    }

    public string ExportJson(string filePath)
    {
        try
        {
            File.WriteAllText(filePath, _jsonText);
            return "success";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public string ImportJson(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            JsonText = content;
            return "success";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
