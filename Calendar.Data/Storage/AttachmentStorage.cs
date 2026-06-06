using Calendar.Core.Storage;

namespace Calendar.Data.Storage;

public sealed class AttachmentStorage : IAttachmentStorage
{
    private readonly string _root;

    public AttachmentStorage()
    {
        _root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CalendarWidget", "attachments");
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default)
    {
        var ext = string.IsNullOrEmpty(extension)
            ? string.Empty
            : (extension.StartsWith('.') ? extension : "." + extension);

        var name = Guid.NewGuid().ToString("N") + ext;
        var full = Path.Combine(_root, name);

        await using (var fs = File.Create(full))
            await content.CopyToAsync(fs, ct);

        return name;
    }

    public string GetFullPath(string relativePath) => Path.Combine(_root, relativePath);

    public void Delete(string relativePath)
    {
        try
        {
            var full = Path.Combine(_root, relativePath);
            if (File.Exists(full)) File.Delete(full);
        }
        catch
        {
            // best effort; orphaned files are harmless
        }
    }
}
