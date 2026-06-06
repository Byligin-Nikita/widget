namespace Calendar.Core.Storage;

public interface IAttachmentStorage
{
    /// <summary>Saves the stream into the attachments folder under a new unique name.
    /// Returns the relative path (stored file name) to persist in the DB.</summary>
    Task<string> SaveAsync(Stream content, string extension, CancellationToken ct = default);

    /// <summary>Resolves a relative path to an absolute path on disk.</summary>
    string GetFullPath(string relativePath);

    void Delete(string relativePath);
}
