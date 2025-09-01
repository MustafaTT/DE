using DossierExplorer.Domain.Entities;
using DossierExplorer.Domain.Interfaces;

namespace DossierExplorer.Application.Services;

public class FileService : IFileService
{
    // In-memory scaffold for demonstration; replace with actual implementation.
    private readonly List<FileItem> _files = new();

    public Task<IEnumerable<FileItem>> GetFilesAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var result = _files.Where(f => f.Path.StartsWith(directoryPath));
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<FileItem?> GetFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var file = _files.FirstOrDefault(f => f.Path == filePath);
        return Task.FromResult(file);
    }

    public Task<FileItem> CreateFileAsync(string directoryPath, string fileName, CancellationToken cancellationToken = default)
    {
        var file = new FileItem
        {
            Id = Guid.NewGuid().ToString(),
            Name = fileName,
            Path = $"{directoryPath}/{fileName}",
            IsDirectory = false,
            Size = 0,
            LastModified = DateTime.UtcNow
        };
        _files.Add(file);
        return Task.FromResult(file);
    }

    public Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        _files.RemoveAll(f => f.Path == filePath);
        return Task.CompletedTask;
    }

    public Task<FileItem> RenameFileAsync(string filePath, string newName, CancellationToken cancellationToken = default)
    {
        var file = _files.FirstOrDefault(f => f.Path == filePath);
        if (file != null)
        {
            file.Name = newName;
            file.Path = $"{System.IO.Path.GetDirectoryName(filePath)}/{newName}";
            file.LastModified = DateTime.UtcNow;
        }
        return Task.FromResult(file!);
    }
}