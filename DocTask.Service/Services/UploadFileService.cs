using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DocTask.Core.Dtos.UploadFile;
using DocTask.Core.Interfaces.Repositories;
using DocTask.Core.Interfaces.Services;
using DocTask.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using DocTask.Core.Paginations;

namespace DocTask.Service.Services
{
    public class UploadFileService : IUploadFileService
    {
        private readonly IUploadFileRepository _uploadFileRepository;

        private readonly Cloudinary _cloudinary;

        public UploadFileService(IUploadFileRepository uploadFileRepository, Cloudinary cloudinary)
        {
            _uploadFileRepository = uploadFileRepository;
            _cloudinary = cloudinary;
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

            const long maxFileSize = 3 * 1024 * 1024; // 3 MB
            if (request.File.Length > maxFileSize)
            {
                throw new ArgumentException($"File size exceeds the maximum limit of {maxFileSize / (1024 * 1024)} MB");
            }

            List<string> validExtensions = new List<string>()
            {
                ".jpg", ".png", ".gif", // Image
                ".pdf", ".txt",         // Documents
                ".doc", ".docx",        // Word
                ".xls", ".xlsx",        // Excel
            };
            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!validExtensions.Contains(fileExtension))
            {
                throw new ArgumentException($"Extension is not valid({string.Join(',', validExtensions)})");
            }

            // Name
            // string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            // string cleanFileName = Path.GetFileNameWithoutExtension(request.File.FileName);
            // string nameExtension = Path.GetExtension(request.File.FileName);
            // var uniqueFileName = $"{cleanFileName}_user{userId}_at_{timestamp}{nameExtension}";

            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";

            // Upload lên Cloudinary
            await using var stream = request.File.OpenReadStream();
            var uploadCloud = new CloudinaryDotNet.Actions.RawUploadParams()
            {
                File = new CloudinaryDotNet.FileDescription(uniqueFileName, stream),
                PublicId = Path.GetFileNameWithoutExtension(uniqueFileName),
                Overwrite = true,
                Folder = "upload"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadCloud);
            if (uploadResult.Error != null)
            {
                throw new ArgumentException($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            // URL
            var fileUrl = uploadResult.SecureUrl.AbsoluteUri;

            var uploadFile = new Uploadfile
            {
                FileName = request.File.FileName,
                FilePath = fileUrl,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow
            };

            var savedFile = await _uploadFileRepository.CreateAsync(uploadFile);

            return new UploadFileDto
            {
                FileId = savedFile.FileId,
                FileName = savedFile.FileName,
                FilePath = savedFile.FilePath,
                UploadedBy = savedFile.UploadedBy,
                UploadedAt = savedFile.UploadedAt,
                FileSize = request.File.Length,
                ContentType = request.File.ContentType,
            };
        }

        public async Task<UploadFileDto?> GetFileByIdAsync(int fileId)
        {
            var file = await _uploadFileRepository.GetByIdAsync(fileId);
            if (file == null)
            {
                return null;
            }

            var fileInfo = new FileInfo(file.FilePath);

            return new UploadFileDto
            {
                FileId = file.FileId,
                FileName = file.FileName,
                FilePath = file.FilePath,
                UploadedBy = file.UploadedBy,
                UploadedAt = file.UploadedAt,
                FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                ContentType = GetContentType(file.FileName),
            };
        }

        public async Task<List<UploadFileDto>> GetFileByUserIdAsync(int userId)
        {
            var files = await _uploadFileRepository.GetByUserAsync(userId);
            if (files == null)
            {
                return null;
            }

            return files.Select(f =>
            {
                var fileInfo = new FileInfo(f.FilePath);

                return new UploadFileDto
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FilePath = f.FilePath,
                    UploadedBy = f.UploadedBy,
                    UploadedAt = f.UploadedAt,
                    FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                    ContentType = GetContentType(f.FileName),
                };
            }).ToList();
        }

        public async Task<PaginatedList<UploadFileDto>> GetFileByUserIdPaginatedAsync(int userId, PageOptionsRequest pageOptions)
        {
            var paginated = await _uploadFileRepository.GetByUserPaginatedAsync(userId, pageOptions);
            var dtoList = paginated.Items.Select(f =>
            {
                var fileInfo = new FileInfo(f.FilePath);
                return new UploadFileDto
                {
                    FileId = f.FileId,
                    FileName = f.FileName,
                    FilePath = f.FilePath,
                    UploadedBy = f.UploadedBy,
                    UploadedAt = f.UploadedAt,
                    FileSize = fileInfo.Exists ? fileInfo.Length : 0,
                    ContentType = GetContentType(f.FileName),
                };
            }).ToList();

            return new PaginatedList<UploadFileDto>(dtoList, paginated.MetaData);
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".txt" => "text/plain",
                _ => "application/octet-stream",
            };
        }

        public async Task<byte[]?> DownloadFileAsync(int fileId)
        {
            var file = await _uploadFileRepository.GetByIdAsync(fileId);
            if (file == null || string.IsNullOrEmpty(file.FilePath))
            {
                return null;
            }

            if (file.FilePath.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                using var httpClient = new HttpClient();
                try
                {
                    return await httpClient.GetByteArrayAsync(file.FilePath);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }

        public async Task<bool> DeleteFileAsync(int fileId, int userId)
        {
            var file = await _uploadFileRepository.GetByIdAndUserIdAsync(fileId, userId);
            if (file == null)
            {
                return false;
            }

            return await _uploadFileRepository.DeleteAsync(fileId);
        }
    }
}