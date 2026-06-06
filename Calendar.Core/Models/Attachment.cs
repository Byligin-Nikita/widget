namespace Calendar.Core.Models;

public enum AttachmentOwnerType
{
    Note = 0,
    Task = 1
}

public sealed class Attachment : SyncEntityBase
{
    public Guid OwnerId { get; set; }
    public AttachmentOwnerType OwnerType { get; set; }

    /// <summary>Original display file name (e.g. "screenshot.png").</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Path relative to the attachments storage root (the stored file name).</summary>
    public string RelativePath { get; set; } = string.Empty;

    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public bool IsImage { get; set; }
}
