using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using System.Text;
using System.Text.Json;

namespace OcrConsoleApp;

public static class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            string rootDocumentPath = Path.Combine(AppContext.BaseDirectory, "docs");
            string inputFolder = Path.Combine(rootDocumentPath, "input");
            string[] inputFiles = Directory.GetFiles(inputFolder, "*.pdf");
            if (inputFiles.Length == 0)
            {
                Console.WriteLine($"No PDF files found in {inputFolder}");
                return;
            }

            // Load configuration
            var configuration = LoadConfiguration();
            var mistralApiKey = configuration["MistralOCR:ApiKey"] ?? throw new InvalidOperationException("Mistral API key not configured");
            var mistralEndpoint = configuration["MistralOCR:Endpoint"] ?? "https://api.mistral.ai/v1/chat/completions";

            foreach (var inputPath in inputFiles)
            {
                string inputFileName = Path.GetFileName(inputPath);
                string outputPath = Path.Combine(rootDocumentPath, "output", Path.GetFileNameWithoutExtension(inputPath));

                Console.WriteLine($"Processing: {inputFileName}");
                Console.WriteLine($"Input path: {inputPath}");

                // Check if input file exists
                if (!File.Exists(inputPath))
                {
                    Console.WriteLine($"Error: Input file not found at {inputPath}");
                    continue;
                }

                // Split PDF into images
                Console.WriteLine($"Splitting PDF into images...");
                string imagesOutputDir = Path.Combine(outputPath, "0.SplitPdfToImages");
                string mdMistralOutputDir = Path.Combine(outputPath, "1.MistralOCR", "markdown");

                Directory.CreateDirectory(imagesOutputDir);
                Directory.CreateDirectory(mdMistralOutputDir);

                var pageImageFiles = SplitPdfToImages(inputPath, imagesOutputDir);

                foreach (var pageImageFile in pageImageFiles)
                {
                    var mistralResult = await PerformMistralOcrAsync(mistralApiKey, mistralEndpoint, pageImageFile);

                    // Save Mistral OCR result as md file
                    string mistralMdOutputPath = Path.Combine(mdMistralOutputDir, $"{Path.GetFileNameWithoutExtension(pageImageFile)}.md");
                    await SaveMistralOcrMarkdownToFileAsync(mistralMdOutputPath, mistralResult);
                }

                Console.WriteLine("Processing completed successfully!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
    }

    static List<string> SplitPdfToImages(string pdfFilePath, string outputDir)
    {
        var outputFiles = new List<string>();

        byte[] pdfBytes = File.ReadAllBytes(pdfFilePath);

        using var docReader = DocLib.Instance.GetDocReader(pdfBytes, new PageDimensions(1080, 1440));
        int pageCount = docReader.GetPageCount();

        for (int i = 0; i < pageCount; i++)
        {
            using var pageReader = docReader.GetPageReader(i);

            var rawBytes = pageReader.GetImage();
            int width = pageReader.GetPageWidth();
            int height = pageReader.GetPageHeight();

            using var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            System.Runtime.InteropServices.Marshal.Copy(rawBytes, 0, bitmap.GetPixels(), rawBytes.Length);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            string outputPath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(pdfFilePath)}-{i + 1}.png");

            using var fs = File.OpenWrite(outputPath);
            data.SaveTo(fs);

            Console.WriteLine($"Saved: {outputPath}");
            outputFiles.Add(outputPath);
        }

        Console.WriteLine("✅ All pages converted.");

        return outputFiles;
    }

    //private static async Task<(string, MistralOcrResponse)> PerformMistralOcrAsync(string apiKey, string endpoint, string filePath)
    private static async Task<string> PerformMistralOcrAsync(string apiKey, string endpoint, string filePath)
    {
        Console.WriteLine("Performing OCR with Mistral..."+ filePath);

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        // Convert image to base64
        var imageBytes = await File.ReadAllBytesAsync(filePath);
        var base64Image = Convert.ToBase64String(imageBytes);
        var imageExtension = Path.GetExtension(filePath).ToLowerInvariant();
        var mimeType = imageExtension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/png"
        };

        var requestBody = new
        {
            model = "mistral-ocr-latest",
            document = new
            {
                type = "image_url",
                image_url = $"data:{mimeType};base64,{base64Image}"
            },
            include_image_base64 = true
        };

        var jsonContent = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await httpClient.PostAsync(endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            return responseContent;
        }

        throw new Exception($"Mistral OCR API request failed: {response.StatusCode} - {responseContent}");
    }

    // save mistral as md
    private static async Task SaveMistralOcrMarkdownToFileAsync(string outputPath, string ocrResult)
    {
        await File.WriteAllTextAsync(outputPath, ocrResult);
    }
}