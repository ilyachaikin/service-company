using ServiceCompany.Application.Common.Interfaces;

namespace ServiceCompany.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public LocalFileStorageService(string storagePath)
    {
        _storagePath = storagePath;
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct)
    {
        var ext        = Path.GetExtension(fileName);
        var uniqueName = $"{Guid.NewGuid()}{ext}";
        var fullPath   = Path.Combine(_storagePath, uniqueName);

        await using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, ct);

        return uniqueName;
    }

    public string GetFullPath(string uniqueName) =>
        Path.Combine(_storagePath, uniqueName);
}

public class DateTimeService : IDateTimeService
{
    public DateTime UtcNow => DateTime.UtcNow;
}
