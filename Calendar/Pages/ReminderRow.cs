using Microsoft.UI.Xaml;

namespace Calendar.Pages;

public sealed class ReminderRow
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string WhenText { get; init; } = "";
    public bool IsOverdue { get; init; }

    public Visibility OverdueVisibility => IsOverdue ? Visibility.Visible : Visibility.Collapsed;
}
