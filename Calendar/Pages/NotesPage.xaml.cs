using System.Collections.ObjectModel;
using System.Linq;
using Calendar.Controls;
using Calendar.Core.Models;
using Calendar.Services;
using Calendar.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace Calendar.Pages;

public sealed partial class NotesPage : Page
{
    private readonly ObservableCollection<NoteRowViewModel> _rows = new();

    public NotesPage()
    {
        InitializeComponent();
        NotesItems.ItemsSource = _rows;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        await ReloadAsync();
        if (e.Parameter as string == "new")
            await StartNewNoteAsync();
    }

    protected override async void OnNavigatedFrom(NavigationEventArgs e)
    {
        foreach (var vm in _rows.Where(r => r.IsExpanded))
            await SaveAsync(vm);
        base.OnNavigatedFrom(e);
    }

    public async Task ReloadAsync()
    {
        var notes = await AppHost.Notes.GetAllAsync();
        _rows.Clear();
        foreach (var n in notes.OrderByDescending(n => n.UpdatedAt))
        {
            var vm = new NoteRowViewModel(n);
            var atts = await AppHost.Attachments.GetForOwnerAsync(n.Id);
            vm.Badges = NoteBadges.Build(atts);
            _rows.Add(vm);
        }
        EmptyHint.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task StartNewNoteAsync()
    {
        var note = new Note { Title = "Новая заметка", Content = string.Empty };
        await AppHost.Notes.SaveAsync(note);
        await ReloadAsync();

        var vm = _rows.FirstOrDefault(r => r.Id == note.Id);
        if (vm is null) return;
        // Defer so the Expander container exists (collapsed) first, then the
        // false->true transition fires Expanding (lazy-load + focus).
        DispatcherQueue.TryEnqueue(() => vm.IsExpanded = true);
    }

    private void Expander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        if (sender.DataContext is not NoteRowViewModel vm) return;

        // Accordion: only one note open at a time.
        foreach (var other in _rows)
            if (!ReferenceEquals(other, vm))
                other.IsExpanded = false;

        DispatcherQueue.TryEnqueue(async () =>
        {
            if (!vm.AttachmentsLoaded)
            {
                var ac = FindDescendant<AttachmentsControl>(sender);
                if (ac is not null)
                {
                    vm.AttachmentsLoaded = true;
                    await ac.LoadAsync(vm.Id, AttachmentOwnerType.Note);
                }
            }
            FindDescendant<TextBox>(sender)?.Focus(FocusState.Programmatic);
        });
    }

    private async void Expander_Collapsed(Expander sender, ExpanderCollapsedEventArgs args)
    {
        if (sender.DataContext is not NoteRowViewModel vm) return;
        await SaveAsync(vm);
        var atts = await AppHost.Attachments.GetForOwnerAsync(vm.Id);
        vm.Badges = NoteBadges.Build(atts);
    }

    private async void Title_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is NoteRowViewModel vm)
        {
            vm.Title = tb.Text;
            await SaveAsync(vm);
        }
    }

    private async void Content_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb && tb.DataContext is NoteRowViewModel vm)
        {
            vm.Content = tb.Text;
            await SaveAsync(vm);
        }
    }

    private async void New_Click(object sender, RoutedEventArgs e) => await StartNewNoteAsync();

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not NoteRowViewModel vm) return;

        var atts = await AppHost.Attachments.GetForOwnerAsync(vm.Id);
        foreach (var a in atts)
        {
            await AppHost.Attachments.DeleteAsync(a.Id);
            AppHost.AttachmentStorage.Delete(a.RelativePath);
        }
        await AppHost.Notes.DeleteAsync(vm.Id);
        _rows.Remove(vm);
        EmptyHint.Visibility = _rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static async Task SaveAsync(NoteRowViewModel vm)
    {
        vm.Model.Touch();
        await AppHost.Notes.SaveAsync(vm.Model);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) return match;
            var deeper = FindDescendant<T>(child);
            if (deeper is not null) return deeper;
        }
        return null;
    }
}
