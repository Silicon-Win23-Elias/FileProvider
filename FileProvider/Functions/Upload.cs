using Data.Contexts;
using Data.Entities;
using FileProvider.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FileProvider.Functions
{
    public class Upload(ILogger<Upload> logger, FileService fileService)
    {
        private readonly ILogger<Upload> _logger = logger;
        private readonly FileService _fileService = fileService;

        [Function("Upload")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            try
            {
                if (req.Form.Files["file"] is IFormFile file)
                {
                    var containerName = !string.IsNullOrEmpty(req.Query["containerName"]) ? req.Query["containerName"].ToString() : "files";

                    var fileEntity = new FileEntity
                    {
                        FileName = _fileService.SetFileName(file),
                        ContentType = file.ContentType,
                        ContainerName = containerName,

                    };

                    await _fileService.SetBlobContainerAsync(fileEntity.FileName);
                    var filePath = await _fileService.UploadFileAsync(file, fileEntity);
                    fileEntity.FilePath = filePath;

                    await _fileService.SaveToDB(fileEntity);
                    return new OkObjectResult(fileEntity);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return new BadRequestResult();
        }
    }
}
