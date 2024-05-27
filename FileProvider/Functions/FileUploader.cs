using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace FileProvider.Functions;

public class FileUploader(ILogger<FileUploader> logger)
{
    private readonly ILogger<FileUploader> _logger = logger;

    [Function("FileUploader")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        try
        {
            if (req.Form.Files["file"] is IFormFile file)
            {
                string connectionString = Environment.GetEnvironmentVariable("ConnectionString")!;
                string containerName = Environment.GetEnvironmentVariable("FileContainerName")!;

                BlobServiceClient client = new BlobServiceClient(connectionString);
                BlobContainerClient container = client.GetBlobContainerClient(containerName);
                await container.CreateIfNotExistsAsync();

                string fileName = $"{Guid.NewGuid()}_{file.FileName}";
                BlobClient blob = container.GetBlobClient(fileName);

                using Stream stream = file.OpenReadStream();
                await blob.UploadAsync(stream, true);

                return new OkObjectResult(blob.Uri);
            }

            return new BadRequestObjectResult("No file to upload");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        return new BadRequestResult();
    }
}
