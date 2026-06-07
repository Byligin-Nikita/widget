using Calendar.Core.Models;
using Calendar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class NotesPage : Page
{
    private List<Note> _notes = [];
    private Note? _current;
    private bool _suppress;

    public NotesPage() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter as string == "new")
            await StartNewNoteAsync();
        else
            await ReloadAsync();
    }

    public async Task ReloadAsync()
    {
        _notes = [.. await AppHost.Notes.GetAllAsync()];

        _suppress = true;
        NotesList.ItemsSource = _notes;
        _suppress = false;

        if (_current is not null)
            _current = _notes.FirstOrDefault(n => n.Id == _current.Id);
        if (_current is null && _notes.Count > 0)
            _current = _notes[0];

        _suppress = true;
        NotesList.SelectedItem = _current;
        _suppress = false;

        await LoadEditorAsync();
    }

    private async Task LoadEditorAsync()
    {
        var has = _current is not null;
        TitleBox.IsEnabled = ContentBox.IsEnabled = Attachments.IsEnabled = DeleteBtn.IsEnabled = has;
        EmptyHint.Visibility = has ? Visibility.Collapsed : Visibility.Visible;

        TitleBox.Text = _current?.Title ?? string.Empty;
        ContentBox.Text = _current?.Content ?? string.Empty;

        if (has)
            await Attachments.LoadAsync(_current!.Id, AttachmentOwnerType.Note);
        else
            Attachments.Reset();
    }

    private async void New_Click(object sender, RoutedEventArgs e) => await StartNewNoteAsync();

    public async Task StartNewNoteAsync()
    {
        var note = new Note { Title = "Новая заметка", Content = string.Empty };
        await AppHost.Notes.SaveAsync(note);
        _current = note;
        await ReloadAsync();
        TitleBox.Focus(FocusState.Programmatic);
        TitleBox.SelectAll();
    }

    private async void NotesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppress) return;
        _current = NotesList.SelectedItem as Note;
        await LoadEditorAsync();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        _current.Title = string.IsNullOrWhiteSpace(TitleBox.Text) ? "Без названия" : TitleBox.Text.Trim();
        _current.Content = ContentBox.Text ?? string.Empty;
        await AppHost.Notes.SaveAsync(_current);
        await ReloadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_current is null) return;
        await AppHost.Notes.DeleteAsync(_current.Id);
        _current = null;
        await ReloadAsync();
    }
}
