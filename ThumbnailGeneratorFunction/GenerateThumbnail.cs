using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Azure.Storage.Blobs;
namespace ThumbnailGeneratorFunction;

 public class GenerateThumbnail
    {
        private readonly ILogger _logger;

        public GenerateThumbnail(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GenerateThumbnail>();
        }

        [Function("GenerateThumbnail")]
        public async Task Run(
            [BlobTrigger("image-input/{name}", Connection = "AzureWebJobsStorage")] Stream inputBlob,
            string name)
        {
            _logger.LogInformation($"Blob trigger function processing blob: {name}");

            // Load image using ImageSharp
            using var image = await Image.LoadAsync(inputBlob);

            // Resize to thumbnail (e.g. 150px wide)
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(150, 0), // Width 150, height auto
                Mode = ResizeMode.Max
            }));

            // Prepare output stream
            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream);
            outputStream.Position = 0;

            // Upload to output container
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
            var outputBlobClient = new BlobClient(connectionString, "image-output", name);
            await outputBlobClient.UploadAsync(outputStream, overwrite: true);

            _logger.LogInformation($"Thumbnail saved to image-output/{name}");
        }
    }