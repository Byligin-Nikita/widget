using Calendar.Core.Models;
using Windows.UI.Text;

namespace Calendar.ViewModels;

public sealed class TaskRowViewModel : BindableBase
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";

    private bool _isCompleted;
    public bool IsCompleted
    {
        get => _isCompleted;
        set { SetProperty(ref _isCompleted, value); UpdateDisplay(); }
    }

    private int _completionPercent;
    public int CompletionPercent
    {
        get => _completionPercent;
        set { SetProperty(ref _completionPercent, value); OnPropertyChanged(nameof(PercentText)); }
    }

    public string PercentText => $"{CompletionPercent}%";
    public TextDecorations StrikeThrough => IsCompleted ? TextDecorations.Strikethrough : TextDecorations.None;

    public void UpdateDisplay()
    {
        OnPropertyChanged(nameof(StrikeThrough));
        OnPropertyChanged(nameof(PercentText));
    }

    public static TaskRowViewModel From(TaskItem t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        IsCompleted = t.IsCompleted,
        CompletionPercent = t.CompletionPercent
    };
}

public abstract class BindableBase : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new(name));
        return true;
    }

    protected void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new(name));
}
