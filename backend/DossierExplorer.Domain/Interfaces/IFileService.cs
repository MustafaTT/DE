using DossierExplorer.Domain.Entities;

namespace DossierExplorer.Domain.Interfaces;

public interface IFileService
{
    Task<IEnumerable<FileItem>> GetFilesAsync(string directoryPath, CancellationToken cancellationToken = default);
    Task<FileItem?> GetFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<FileItem> CreateFileAsync(string directoryPath, string fileName, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    Task<FileItem> RenameFileAsync(string filePath, string newName, CancellationToken cancellationToken = default);
}