using Docnet.Core;
using Docnet.Core.Models;
using Microsoft.Extensions.Configuration;
using SkiaSharp;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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

            foreach (var inputPath in inputFiles.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                await ProcessPdfFile(inputPath, rootDocumentPath, mistralApiKey, mistralEndpoint);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }

    // ประมวลผล PDF ทีละไฟล์
    private static async Task ProcessPdfFile(string inputPath, string rootDocumentPath, string mistralApiKey, string mistralEndpoint)
    {
        string inputFileName = Path.GetFileName(inputPath);
        string outputPath = Path.Combine(rootDocumentPath, "output", Path.GetFileNameWithoutExtension(inputPath));

        PrepareOutputDirectory(outputPath);

        Console.WriteLine($"Processing: {inputFileName}");
        Console.WriteLine($"Input path: {inputPath}");

        if (!File.Exists(inputPath))
        {
            Console.WriteLine($"Error: Input file not found at {inputPath}");
            return;
        }

        // Split PDF เป็นรูปภาพ
        Console.WriteLine($"Splitting PDF into images...");
        string imagesOutputDir = Path.Combine(outputPath, "0.SplitPdfToImages");
        string mdMistralOutputDir = Path.Combine(outputPath, "1.MistralOCR");
        Directory.CreateDirectory(imagesOutputDir);
        Directory.CreateDirectory(mdMistralOutputDir);

        var pageImageFiles = SplitPdfToImages(inputPath, imagesOutputDir);
        var allPages = new List<MistralOcrPage>();

        // OCR ทีละหน้า
        for (int i = 0; i < pageImageFiles.Count; i++)
        {
            var pageImageFile = pageImageFiles[i];
            var mistralResult = await PerformMistralOcrAsync(mistralApiKey, mistralEndpoint, pageImageFile);
            var mistralResponse = JsonSerializer.Deserialize<MistralOcrResponse>(mistralResult);
            if (mistralResponse?.pages != null)
            {
                foreach (var page in mistralResponse.pages)
                {
                    page.index = i; // Force correct page index
                    allPages.Add(page);
                }
            }
        }

        // รวมผลลัพธ์และบันทึก
        var combinedResponse = new MistralOcrResponse { pages = allPages };
        await SaveMistralOcrResultToPageFoldersAsync(mdMistralOutputDir, combinedResponse);
        Console.WriteLine("Processing completed successfully!");
    }

    // เตรียม output directory (ลบของเก่า ถ้ามี)
    private static void PrepareOutputDirectory(string outputPath)
    {
        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, true);
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

    private static async Task<string> PerformMistralOcrAsync(string apiKey, string endpoint, string filePath)
    {
        Console.WriteLine("Performing OCR with Mistral..." + filePath);

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

        throw new InvalidOperationException($"Mistral OCR API request failed: {response.StatusCode} - {responseContent}");
    }

    public class MistralOcrResponse {
        public List<MistralOcrPage>? pages { get; set; }
    }
    public class MistralOcrPage {
        public int index { get; set; }
        public string? markdown { get; set; }
        public List<MistralOcrImage>? images { get; set; }
    }
    public class MistralOcrImage {
        public string? id { get; set; }
        public string? image_base64 { get; set; }
    }

    // save mistral as md
    private static async Task SaveMistralOcrResultToPageFoldersAsync(string outputDir, MistralOcrResponse mistral)
    {
        if (mistral?.pages == null) return;
        foreach (var page in mistral.pages)
        {
            string pageDir = Path.Combine(outputDir, $"page-{page.index + 1}");
            Directory.CreateDirectory(pageDir);
            await SaveMarkdownForPageAsync(page, pageDir);
            await SaveImagesForPageAsync(page, pageDir);
        }
    }

    private static async Task SaveMarkdownForPageAsync(MistralOcrPage page, string pageDir)
    {
        string mdPath = Path.Combine(pageDir, $"page-{page.index + 1}.md");
        string markdown = page.markdown ?? "";
        // Post-process: จัดการตาราง
        if (ContainsMarkdownTable(markdown))
        {
            markdown = FixMarkdownTableFormat(markdown);
        }
        // Post-process: จัดการ tag image ให้ขึ้นบรรทัดใหม่ เฉพาะกรณีมี image
        if (ContainsMarkdownImage(markdown))
        {
            markdown = FixImageTagNewline(markdown);
        }
        await File.WriteAllTextAsync(mdPath, markdown);
    }

    // ตรวจสอบว่า markdown มี image tag หรือไม่
    private static bool ContainsMarkdownImage(string markdown)
    {
        return markdown.Contains("![");
    }

    // ให้ tag รูปภาพ ![...](...) ขึ้นบรรทัดใหม่และมีบรรทัดว่างคั่นก่อนหน้าเสมอ
    private static string FixImageTagNewline(string markdown)
    {
        var pattern = @"!\[.*?\]\(.*?\)";
        var lines = markdown.Split('\n');
        var newLines = new List<string>();
        foreach (var line in lines)
        {
            if (line.Contains("!["))
            {
                var result = SplitLineByImageTagsWithBlankLine(line, pattern, newLines.Count > 0 ? newLines[^1] : null);
                newLines.AddRange(result);
            }
            else
            {
                newLines.Add(line);
            }
        }
        return string.Join("\n", newLines);
    }

    // แยกข้อความกับ tag รูปภาพออกเป็นบรรทัดใหม่ และแทรกบรรทัดว่างก่อน tag รูปภาพ
    private static IEnumerable<string> SplitLineByImageTagsWithBlankLine(string line, string pattern, string? prevLine)
    {
        int lastIndex = 0;
        bool addedBlank = false;
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(line, pattern))
        {
            if (match.Index > lastIndex)
            {
                var before = line.Substring(lastIndex, match.Index - lastIndex).Trim();
                if (!string.IsNullOrEmpty(before))
                    yield return before;
            }
            // แทรกบรรทัดว่างก่อน tag รูปภาพ ถ้าบรรทัดก่อนหน้าไม่ว่างและยังไม่ได้แทรก
            if (!addedBlank && (prevLine != null && !string.IsNullOrWhiteSpace(prevLine)))
            {
                yield return "";
                addedBlank = true;
            }
            yield return match.Value.Trim();
            lastIndex = match.Index + match.Length;
        }
        if (lastIndex < line.Length)
        {
            var after = line.Substring(lastIndex).Trim();
            if (!string.IsNullOrEmpty(after))
                yield return after;
        }
    }

    // ตรวจสอบว่า markdown มีตารางหรือไม่
    private static bool ContainsMarkdownTable(string markdown)
    {
        var lines = markdown.Split('\n');
        bool hasTableHeader = false;
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith('|') && line.Contains("---"))
            {
                hasTableHeader = true;
                break;
            }
        }
        return hasTableHeader;
    }

    // รวม cell ที่ขึ้นบรรทัดใหม่ให้อยู่ในบรรทัดเดียว (fix เฉพาะตาราง)
    private static string FixMarkdownTableFormat(string markdown)
    {
        var lines = markdown.Split('\n');
        var newLines = new List<string>();
        bool inTable = false;
        StringBuilder currentRow = new StringBuilder();
        foreach (var line in lines)
        {
            if (line.Trim().StartsWith('|'))
            {
                inTable = true;
                if (currentRow.Length > 0)
                {
                    newLines.Add(currentRow.ToString());
                    currentRow.Clear();
                }
                currentRow.Append(line.Trim());
            }
            else if (inTable && !string.IsNullOrWhiteSpace(line))
            {
                currentRow.Append(" " + line.Trim());
            }
            else
            {
                if (currentRow.Length > 0)
                {
                    newLines.Add(currentRow.ToString());
                    currentRow.Clear();
                }
                inTable = false;
                newLines.Add(line);
            }
        }
        if (currentRow.Length > 0)
            newLines.Add(currentRow.ToString());
        return string.Join("\n", newLines);
    }

    private static async Task SaveImagesForPageAsync(MistralOcrPage page, string pageDir)
    {
        if (page.images == null) return;
        foreach (var img in page.images)
        {
            if (!string.IsNullOrEmpty(img.image_base64) && !string.IsNullOrEmpty(img.id))
            {
                var base64 = img.image_base64;
                var commaIdx = base64.IndexOf(",");
                if (commaIdx >= 0) base64 = base64[(commaIdx + 1)..];
                byte[] imgBytes = Convert.FromBase64String(base64);
                string imgPath = Path.Combine(pageDir, img.id);
                await File.WriteAllBytesAsync(imgPath, imgBytes);
            }
        }
    }
}