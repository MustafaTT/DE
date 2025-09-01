using DossierExplorer.Domain.Entities;
using DossierExplorer.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DossierExplorer.API.Controllers;

[ApiController]
[Route("api/files")]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet]
    public async Task<IEnumerable<FileItem>> GetFiles([FromQuery] string directoryPath = "/")
    {
        return await _fileService.GetFilesAsync(directoryPath);
    }

    [HttpPost]
    public async Task<ActionResult<FileItem>> CreateFile([FromQuery] string directoryPath, [FromQuery] string fileName)
    {
        var file = await _fileService.CreateFileAsync(directoryPath, fileName);
        return CreatedAtAction(nameof(GetFile), new { filePath = file.Path }, file);
    }

    [HttpGet("{*filePath}")]
    public async Task<ActionResult<FileItem>> GetFile(string filePath)
    {
        var file = await _fileService.GetFileAsync(filePath);
        if (file == null) return NotFound();
        return file;
    }

    [HttpDelete("{*filePath}")]
    public async Task<IActionResult> DeleteFile(string filePath)
    {
        await _fileService.DeleteFileAsync(filePath);
        return NoContent();
    }

    [HttpPut("{*filePath}")]
    public async Task<ActionResult<FileItem>> RenameFile(string filePath, [FromQuery] string newName)
    {
        var file = await _fileService.RenameFileAsync(filePath, newName);
        return Ok(file);
    }
}