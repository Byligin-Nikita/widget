using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Calendar.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Calendar.ViewModels;

/// <summary>One row in the Notes accordion. Wraps the underlying <see cref="Note"/>.</summary>
public sealed class NoteRowViewModel : INotifyPropertyChanged
{
    public Note Model { get; }
    public Guid Id => Model.Id;

    public NoteRowViewModel(Note note) => Model = note;

    public string Title
    {
        get => Model.Title;
        set
        {
            if (Model.Title == value) return;
            Model.Title = value;
            Raise(nameof(Title));
            Raise(nameof(DisplayTitle));
        }
    }

    public string DisplayTitle => string.IsNullOrWhiteSpace(Model.Title) ? "Без названия" : Model.Title;

    public string Content
    {
        get => Model.Content;
        set
        {
            if (Model.Content == value) return;
            Model.Content = value;
            Raise(nameof(Content));
            Raise(nameof(Preview));
            Raise(nameof(PreviewVisibility));
        }
    }

    public string Preview
    {
        get
        {
            var c = (Model.Content ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
            return c.Length <= 80 ? c : c[..80] + "…";
        }
    }

    public Visibility PreviewVisibility => string.IsNullOrWhiteSpace(Preview) ? Visibility.Collapsed : Visibility.Visible;

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set { if (_isExpanded != value) { _isExpanded = value; Raise(nameof(IsExpanded)); } }
    }

    /// <summary>Attachments are loaded into the editor lazily, only once, on first expand.</summary>
    public bool AttachmentsLoaded { get; set; }

    private IReadOnlyList<NoteBadge> _badges = System.Array.Empty<NoteBadge>();
    public IReadOnlyList<NoteBadge> Badges
    {
        get => _badges;
        set { _badges = value; Raise(nameof(Badges)); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>A small overlapping content-type chip shown on a note row.</summary>
public sealed class NoteBadge
{
    public NoteBadge(SolidColorBrush background, string glyph, string letter)
    {
        Background = background;
        Glyph = glyph;
        Letter = letter;
    }

    public SolidColorBrush Background { get; }
    public string Glyph { get; }   // "" when a letter is used
    public string Letter { get; }  // "" when a glyph is used

    public Visibility GlyphVisibility => string.IsNullOrEmpty(Glyph) ? Visibility.Collapsed : Visibility.Visible;
    public Visibility LetterVisibility => string.IsNullOrEmpty(Letter) ? Visibility.Collapsed : Visibility.Visible;
}

/// <summary>Builds distinct content-type chips from a note's attachments.</summary>
public static class NoteBadges
{
    private enum Kind { Word, Excel, PowerPoint, Pdf, Image, Audio, Video, Archive, Text, Other }

    public static IReadOnlyList<NoteBadge> Build(IEnumerable<Attachment> attachments)
    {
        // Distinct kinds, kept in this priority order, capped to keep the row tidy.
        var kinds = attachments
            .Select(KindOf)
            .Distinct()
            .OrderBy(k => (int)k)
            .Take(4)
            .ToList();

        return kinds.Select(MakeBadge).ToList();
    }

    private static Kind KindOf(Attachment a)
    {
        var ext = System.IO.Path.GetExtension(a.FileName).ToLowerInvariant();
        switch (ext)
        {
            case ".doc": case ".docx": case ".rtf": case ".odt": return Kind.Word;
            case ".xls": case ".xlsx": case ".csv": case ".ods": return Kind.Excel;
            case ".ppt": case ".pptx": case ".odp": return Kind.PowerPoint;
            case ".pdf": return Kind.Pdf;
            case ".png": case ".jpg": case ".jpeg": case ".gif": case ".bmp":
            case ".webp": case ".tif": case ".tiff": case ".svg": case ".heic": return Kind.Image;
            case ".mp3": case ".wav": case ".flac": case ".ogg": case ".m4a": return Kind.Audio;
            case ".mp4": case ".mov": case ".avi": case ".mkv": case ".webm": return Kind.Video;
            case ".zip": case ".rar": case ".7z": case ".gz": case ".tar": return Kind.Archive;
            case ".txt": case ".md": case ".log": case ".json": case ".xml": return Kind.Text;
            default: return a.IsImage ? Kind.Image : Kind.Other;
        }
    }

    private static NoteBadge MakeBadge(Kind k) => k switch
    {
        Kind.Word       => new(Brush(0x2B, 0x57, 0x9A), "", "W"),
        Kind.Excel      => new(Brush(0x21, 0x73, 0x46), "", "X"),
        Kind.PowerPoint => new(Brush(0xC4, 0x3E, 0x1C), "", "P"),
        Kind.Pdf        => new(Brush(0xD2, 0x47, 0x26), G(0xE7C3), ""),
        Kind.Image      => new(Brush(0x4F, 0x84, 0xC9), G(0xE8B9), ""),
        Kind.Audio      => new(Brush(0x8E, 0x5B, 0xD9), G(0xEC4F), ""),
        Kind.Video      => new(Brush(0xC2, 0x18, 0x5B), G(0xE714), ""),
        Kind.Archive    => new(Brush(0xB5, 0x8B, 0x00), G(0xE8B7), ""),
        Kind.Text       => new(Brush(0x7C, 0x78, 0x73), G(0xE7C3), ""),
        _               => new(Brush(0x7C, 0x78, 0x73), G(0xE7C3), ""),
    };

    private static string G(int codePoint) => char.ConvertFromUtf32(codePoint);

    private static SolidColorBrush Brush(byte r, byte g, byte b) => new(Color.FromArgb(0xFF, r, g, b));
}
