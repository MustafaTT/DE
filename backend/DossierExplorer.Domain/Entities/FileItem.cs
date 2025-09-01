namespace DossierExplorer.Domain.Entities;

public class FileItem
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Path { get; set; } = default!;
    public bool IsDirectory { get; set; }
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
}