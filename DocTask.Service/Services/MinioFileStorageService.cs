using Amazon.S3;
using Amazon.S3.Model;
using DocTask.Core.Dtos.UploadFile;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Models;
using DocTask.Core.Paginations;
using Microsoft.Extensions.Options;
using System.Web;

namespace DocTask.Core.Services
{
  public class MinioFileStorageService : IFileStorageService
  {
    private readonly IUploadFileRepository _uploadFileRepository;
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly string _endpoint;

    public MinioFileStorageService(
        IAmazonS3 s3Client,
        IOptions<MinioSettings> settings,
        IUploadFileRepository uploadFileRepository)
    {
      _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
      _bucketName = settings.Value.BucketName?.Trim().ToLower() ?? throw new ArgumentNullException("BucketName is null");
      _uploadFileRepository = uploadFileRepository ?? throw new ArgumentNullException(nameof(uploadFileRepository));
      _endpoint = settings.Value.ServiceURL?.TrimEnd('/') ?? "http://localhost:9000";
    }

    private string SanitizeKey(string fileName)
    {
      // Thay dấu cách bằng _ và loại bỏ ký tự không hợp lệ
      var safeName = fileName.Trim().Replace(" ", "_");
      return safeName;
    }

    public async Task<UploadFileDto> UploadFileAsync(UploadFileRequest request, int? userId)
    {
      if (userId == null)
        {
            return null;
        }

      if (request.File == null || request.File.Length == 0)
        {
            throw new ArgumentException("No file provided");
        }
        
      // Kiểm tra định dạng file
      List<string> validExtensions = new List<string>()
    {
        ".jpg", ".png", ".gif", // Image
        ".pdf", ".txt",         // Documents
        ".doc", ".docx",        // Word
        ".xls", ".xlsx",        // Excel
        ".ppt", ".pptx",        // Powerpoint
    };
      var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
      if (!validExtensions.Contains(fileExtension))
        throw new ArgumentException($"Extension is not valid ({string.Join(", ", validExtensions)})");
        
      var safeFileName = $"{Guid.NewGuid()}_{SanitizeKey(request.File.FileName)}";

      // Kiểm tra bucket tồn tại
      var buckets = await _s3Client.ListBucketsAsync();
      if (!buckets.Buckets.Any(b => b.BucketName == _bucketName))
        throw new Exception($"Bucket '{_bucketName}' không tồn tại hoặc client không nhìn thấy!");

      using var stream = request.File.OpenReadStream();
      var putRequest = new PutObjectRequest
      {
        BucketName = _bucketName,
        Key = safeFileName,
        InputStream = stream,
        ContentType = request.File.ContentType
      };

      await _s3Client.PutObjectAsync(putRequest);

      var fileMeta = new Uploadfile
      {
        FileName = request.File.FileName,
        FilePath = safeFileName, // chỉ lưu key an toàn
        UploadedBy = userId,
        UploadedAt = DateTime.UtcNow
      };

      var fileId = await _uploadFileRepository.SaveFileMetadataAsync(fileMeta);

      return new UploadFileDto
      {
        FileId = fileId,
        FileName = fileMeta.FileName,
        FilePath = $"{_endpoint}/{_bucketName}/{HttpUtility.UrlEncode(fileMeta.FilePath)}",
        UploadedBy = fileMeta.UploadedBy,
        UploadedAt = fileMeta.UploadedAt
      };
    }

    public async Task<Stream?> DownloadFileAsync(int fileId)
    {
      var fileMeta = await _uploadFileRepository.GetByIdAsync(fileId);
      if (fileMeta == null)
        throw new FileNotFoundException($"File with id {fileId} not found.");

      var getRequest = new GetObjectRequest
      {
        BucketName = _bucketName,
        Key = fileMeta.FilePath
      };

      var response = await _s3Client.GetObjectAsync(getRequest);
      return response.ResponseStream;
    }

    public async Task<string?> GetFileDownloadLinkAsync(int fileId)
    {
      var fileMeta = await _uploadFileRepository.GetByIdAsync(fileId);
      if (fileMeta == null)
        throw new FileNotFoundException($"File with id {fileId} not found.");

      var getRequest = new GetPreSignedUrlRequest
      {
        BucketName = _bucketName,
        Key = fileMeta.FilePath,
        Expires = DateTime.UtcNow.AddMinutes(15) // Link hợp lệ trong 15 phút
      };

      getRequest.ResponseHeaderOverrides.ContentDisposition = $"attachment; filename=\"{fileMeta.FileName}\"";

      var url = _s3Client.GetPreSignedURL(getRequest);
      return url;
    }

    public async Task<bool> DeleteFileAsync(int fileId, int userId)
    {
      var fileMeta = await _uploadFileRepository.GetByIdAsync(fileId);
      if (fileMeta == null) return false;

      var deleteRequest = new DeleteObjectRequest
      {
        BucketName = _bucketName,
        Key = fileMeta.FilePath
      };

      var response = await _s3Client.DeleteObjectAsync(deleteRequest);
      if (response.HttpStatusCode == System.Net.HttpStatusCode.NoContent)
      {
        await _uploadFileRepository.DeleteAsync(fileId);
        return true;
      }

      return false;
    }

    public async Task<UploadFileDto?> GetFileByIdAsync(int fileId)
    {
      var fileMeta = await _uploadFileRepository.GetByIdAsync(fileId);
      if (fileMeta == null) return null;

      return new UploadFileDto
      {
        FileId = fileMeta.FileId,
        FileName = fileMeta.FileName,
        FilePath = $"{_endpoint}/{_bucketName}/{HttpUtility.UrlEncode(fileMeta.FilePath)}",
        UploadedBy = fileMeta.UploadedBy,
        UploadedAt = fileMeta.UploadedAt
      };
    }

    public async Task<List<UploadFileDto>> GetFileByUserIdAsync(int userId)
    {
      var files = await _uploadFileRepository.GetByUserAsync(userId);
      return files.Select(f => new UploadFileDto
      {
        FileId = f.FileId,
        FileName = f.FileName,
        FilePath = $"{_endpoint}/{_bucketName}/{HttpUtility.UrlEncode(f.FilePath)}",
        UploadedBy = f.UploadedBy,
        UploadedAt = f.UploadedAt
      }).ToList();
    }

    public async Task<PaginatedList<UploadFileDto>> GetFileByUserIdPaginatedAsync(int userId, PageOptionsRequest pageOptions)
    {
      var allFiles = await GetFileByUserIdAsync(userId);
      var items = allFiles
          .Skip((pageOptions.Page - 1) * pageOptions.Size)
          .Take(pageOptions.Size)
          .ToList();

      return new PaginatedList<UploadFileDto>(
          items,
          allFiles.Count,
          pageOptions.Page,
          pageOptions.Size
      );
    }
  }
}
