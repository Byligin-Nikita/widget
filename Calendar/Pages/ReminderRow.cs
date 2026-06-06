namespace Calendar.Pages;

public sealed class ReminderRow
{
    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string WhenText { get; init; } = "";
}
