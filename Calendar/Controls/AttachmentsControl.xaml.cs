using System.Collections.ObjectModel;
using Calendar.Core.Models;
using Calendar.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace Calendar.Controls;

public sealed partial class AttachmentsControl : UserControl
{
    private Guid _ownerId;
    private AttachmentOwnerType _ownerType;

    public ObservableCollection<AttachmentItem> Items { get; } = new();

    public AttachmentsControl()
    {
        InitializeComponent();
    }

    public async Task LoadAsync(Guid ownerId, AttachmentOwnerType ownerType)
    {
        _ownerId = ownerId;
        _ownerType = ownerType;
        Items.Clear();
        var list = await AppHost.Attachments.GetForOwnerAsync(ownerId);
        foreach (var a in list)
            Items.Add(await ToItemAsync(a));
        UpdateHint();
    }

    public void Reset()
    {
        _ownerId = Guid.Empty;
        Items.Clear();
        UpdateHint();
    }

    private void UpdateHint()
        => DropHint.Visibility = Items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private static bool IsImageExtension(string ext) => ext.ToLowerInvariant() switch
    {
        ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" or ".tif" or ".tiff" => true,
        _ => false
    };

    private static async Task<BitmapImage?> MakeThumbAsync(string fullPath)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(fullPath);
            using var stream = await file.OpenAsync(FileAccessMode.Read);
            var bmp = new BitmapImage { DecodePixelWidth = 220 };
            await bmp.SetSourceAsync(stream);
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private async Task<AttachmentItem> ToItemAsync(Attachment a)
    {
        var full = AppHost.AttachmentStorage.GetFullPath(a.RelativePath);
        BitmapImage? thumb = null;
        StorageFile? file = null;
        if (File.Exists(full))
        {
            try { file = await StorageFile.GetFileFromPathAsync(full); } catch { }
            if (a.IsImage) thumb = await MakeThumbAsync(full);
        }
        return new AttachmentItem
        {
            Id = a.Id,
            FileName = a.FileName,
            FullPath = full,
            IsImage = a.IsImage,
            Thumbnail = thumb,
            StoredFile = file
        };
    }

    // Drag attachments OUT to Explorer / other apps.
    private void Items_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        var files = new List<IStorageItem>();
        foreach (var obj in e.Items)
            if (obj is AttachmentItem { StoredFile: { } f }) files.Add(f);

        if (files.Count == 0) { e.Cancel = true; return; }
        e.Data.SetStorageItems(files);
        e.Data.RequestedOperation = DataPackageOperation.Copy;
    }

    private AttachmentItem? Find(object sender)
        => (sender as FrameworkElement)?.Tag is Guid id ? Items.FirstOrDefault(i => i.Id == id) : null;

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        var it = Find(sender);
        if (it is not null && File.Exists(it.FullPath))
            await Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(it.FullPath));
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        var it = Find(sender);
        if (it?.StoredFile is null || App.MainWidget is null) return;

        var ext = Path.GetExtension(it.FileName);
        if (string.IsNullOrEmpty(ext)) ext = ".dat";

        var picker = new FileSavePicker { SuggestedFileName = Path.GetFileNameWithoutExtension(it.FileName) };
        picker.FileTypeChoices.Add(ext.TrimStart('.').ToUpperInvariant(), new List<string> { ext });
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWidget));

        var target = await picker.PickSaveFileAsync();
        if (target is not null)
            await it.StoredFile.CopyAndReplaceAsync(target);
    }

    private async void RemoveMenu_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Guid id) await RemoveByIdAsync(id);
    }

    private async Task AddStorageFileAsync(StorageFile file)
    {
        if (_ownerId == Guid.Empty) return;
        using var ras = await file.OpenReadAsync();
        using var s = ras.AsStreamForRead();
        var ext = Path.GetExtension(file.Name);
        var rel = await AppHost.AttachmentStorage.SaveAsync(s, ext);
        long size = 0;
        try { size = (long)(await file.GetBasicPropertiesAsync()).Size; } catch { }
        var att = new Attachment
        {
            OwnerId = _ownerId,
            OwnerType = _ownerType,
            FileName = file.Name,
            RelativePath = rel,
            ContentType = file.ContentType,
            SizeBytes = size,
            IsImage = IsImageExtension(ext)
        };
        await AppHost.Attachments.SaveAsync(att);
        Items.Add(await ToItemAsync(att));
        UpdateHint();
    }

    private async Task AddBitmapStreamAsync(Stream s)
    {
        if (_ownerId == Guid.Empty) return;
        var rel = await AppHost.AttachmentStorage.SaveAsync(s, ".png");
        long size = 0;
        try { size = new FileInfo(AppHost.AttachmentStorage.GetFullPath(rel)).Length; } catch { }
        var att = new Attachment
        {
            OwnerId = _ownerId,
            OwnerType = _ownerType,
            FileName = $"вставка-{DateTime.Now:yyyyMMdd-HHmmss}.png",
            RelativePath = rel,
            ContentType = "image/png",
            SizeBytes = size,
            IsImage = true
        };
        await AppHost.Attachments.SaveAsync(att);
        Items.Add(await ToItemAsync(att));
        UpdateHint();
    }

    private async void AddFile_Click(object sender, RoutedEventArgs e)
    {
        if (_ownerId == Guid.Empty || App.MainWidget is null) return;
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add("*");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWidget));
        var files = await picker.PickMultipleFilesAsync();
        if (files is null) return;
        foreach (var f in files)
            await AddStorageFileAsync(f);
    }

    private async void Paste_Click(object sender, RoutedEventArgs e)
    {
        if (_ownerId == Guid.Empty) return;
        var dp = Clipboard.GetContent();
        if (dp.Contains(StandardDataFormats.StorageItems))
        {
            foreach (var it in await dp.GetStorageItemsAsync())
                if (it is StorageFile f) await AddStorageFileAsync(f);
        }
        else if (dp.Contains(StandardDataFormats.Bitmap))
        {
            var bmpRef = await dp.GetBitmapAsync();
            using var ras = await bmpRef.OpenReadAsync();
            using var s = ras.AsStreamForRead();
            await AddBitmapStreamAsync(s);
        }
    }

    private void Root_DragOver(object sender, DragEventArgs e)
    {
        if (_ownerId != Guid.Empty &&
            (e.DataView.Contains(StandardDataFormats.StorageItems) || e.DataView.Contains(StandardDataFormats.Bitmap)))
            e.AcceptedOperation = DataPackageOperation.Copy;
        else
            e.AcceptedOperation = DataPackageOperation.None;
    }

    private async void Root_Drop(object sender, DragEventArgs e)
    {
        if (_ownerId == Guid.Empty) return;
        var def = e.GetDeferral();
        try
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                foreach (var it in await e.DataView.GetStorageItemsAsync())
                    if (it is StorageFile f) await AddStorageFileAsync(f);
            }
            else if (e.DataView.Contains(StandardDataFormats.Bitmap))
            {
                var bmpRef = await e.DataView.GetBitmapAsync();
                using var ras = await bmpRef.OpenReadAsync();
                using var s = ras.AsStreamForRead();
                await AddBitmapStreamAsync(s);
            }
        }
        finally
        {
            def.Complete();
        }
    }

    // Single/Ctrl/Shift click selects (for multi-drag); double-click opens.
    private async void Item_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is AttachmentItem it && File.Exists(it.FullPath))
            await Launcher.LaunchFileAsync(await StorageFile.GetFileFromPathAsync(it.FullPath));
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is Guid id) await RemoveByIdAsync(id);
    }

    private async Task RemoveByIdAsync(Guid id)
    {
        var att = await AppHost.Attachments.GetByIdAsync(id);
        await AppHost.Attachments.DeleteAsync(id);
        if (att is not null) AppHost.AttachmentStorage.Delete(att.RelativePath);
        var item = Items.FirstOrDefault(i => i.Id == id);
        if (item is not null) Items.Remove(item);
        UpdateHint();
    }
}

public sealed class AttachmentItem
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public bool IsImage { get; set; }
    public ImageSource? Thumbnail { get; set; }
    public Windows.Storage.StorageFile? StoredFile { get; set; }

    public Visibility ImageVisibility => IsImage ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FileVisibility => IsImage ? Visibility.Collapsed : Visibility.Visible;
}
