using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly string _subscriptionKey;
    private readonly string _endpoint;

    public HomeController(IConfiguration configuration)
    {
        _subscriptionKey = configuration["AzureCognitiveServices:SubscriptionKey"];
        _endpoint = configuration["AzureCognitiveServices:Endpoint"];
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult AnalyzeImage()
    {
        return View();
    }

    [HttpGet]
    public IActionResult LocalImageView()
    {
        return View();
    }

    [HttpGet]
    public IActionResult ImageUrlView()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GenerateThumbnail()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> LocalImageView(IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
        {
            ViewData["Error"] = "Please upload a valid image file.";
            return View();
        }

        // Save the file temporarily
        var filePath = Path.GetTempFileName();
        using (var stream = System.IO.File.Create(filePath))
        {
            await imageFile.CopyToAsync(stream);
        }

        // Analyze the image
        var analysisResult = await AnalyzeLocalImage(filePath);

        // Pass results to the view
        return View("DisplayAnalysisResult", analysisResult);
    }

    [HttpPost]
    public async Task<IActionResult> ImageUrlView(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            ViewData["Error"] = "Please provide a valid image URL.";
            return View(); // This renders the view without a model
        }

        try
        {
            var analysisResult = await AnalyzeImageUrl(imageUrl); // Analyze the image

            // Pass the result as the model to the view
            return View("DisplayAnalysisResult", analysisResult);
        }
        catch (Exception ex)
        {
            ViewData["Error"] = ex.Message;
            return View();
        }
    }

    // Analyze a local image
    private async Task<List<(string Tag, double Confidence)>> AnalyzeLocalImage(string filePath)
    {
        var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
        {
            Endpoint = _endpoint
        };

        using (var imageStream = System.IO.File.OpenRead(filePath))
        {
            var features = new List<VisualFeatureTypes?> { VisualFeatureTypes.Tags };
            var analysis = await client.AnalyzeImageInStreamAsync(imageStream, features);

            return analysis.Tags.Select(tag => (tag.Name, tag.Confidence)).ToList();
        }
    }

    // Analyze an image from a URL
    private async Task<List<(string Tag, double Confidence)>> AnalyzeImageUrl(string imageUrl)
    {
        var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
        {
            Endpoint = _endpoint,
        };

        var features = new List<VisualFeatureTypes?> { VisualFeatureTypes.Tags };
        var analysis = await client.AnalyzeImageAsync(imageUrl, features);

        return analysis.Tags.Select(tag => (tag.Name, tag.Confidence)).ToList();
    }

    private async Task<byte[]> GenerateThumbnail(string imageUrl, int width, int height)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("Image URL cannot be null or empty.");
        }

        var client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(_subscriptionKey))
        {
            Endpoint = _endpoint
        };

        try
        {
            // Call API to generate thumbnail
            var thumbnailStream = await client.GenerateThumbnailAsync(width, height, imageUrl, true);

            if (thumbnailStream == null)
            {
                throw new Exception("Failed to generate thumbnail. The response stream is null.");
            }

            // Copy thumbnail stream to memory and return as byte array
            using (var memoryStream = new MemoryStream())
            {
                await thumbnailStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            Console.WriteLine($"Error generating thumbnail: {ex.Message}");
            throw;
        }
    }

    public async Task<IActionResult> DisplayThumbnail(string imageUrl, int width = 100, int height = 100)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return BadRequest("Image URL cannot be empty.");
        }

        try
        {
            var thumbnailBytes = await GenerateThumbnail(imageUrl, width, height);

            // Convert the byte array to a base64 string to pass to the view
            var base64Thumbnail = Convert.ToBase64String(thumbnailBytes);
            ViewData["Thumbnail"] = $"data:image/jpeg;base64,{base64Thumbnail}";

            return View("DisplayThumbnail"); // Render the thumbnail in a view
        }
        catch (Exception ex)
        {
            // Handle errors (e.g., invalid URL, API issues)
            ViewData["Error"] = ex.Message;
            return View("Error");
        }
    }

}
