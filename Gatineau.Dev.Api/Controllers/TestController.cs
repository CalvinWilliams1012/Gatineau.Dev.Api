using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Playwright;
using OpenAI.Chat;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Http;
using Tesseract;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
using System.Text.RegularExpressions;
using Gatineau.Dev.Api.lib;
using UglyToad.PdfPig.Core;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace Gatineau.Dev.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        public async Task Screenshot(IPage page, string filename)
        {
            using MemoryStream ms = new MemoryStream(await page.ScreenshotAsync());
            #pragma warning disable CA1416 // Validate platform compatibility
            Image.FromStream(ms).Save($"C:/Users/calvi/source/repos/Gatineau.Dev.Api/Gatineau.Dev.Api/download/{filename}.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
            #pragma warning restore CA1416 // Validate platform compatibility
        }

        [HttpGet]
        public async Task<string> Get()
        {
            using (HttpClient client = new HttpClient())
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new() { SlowMo = 2000});
                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://www3.gatineau.ca/servicesenligne/evaluation/Adresse.aspx");
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                string imageSrc = await page.GetByAltText("Captcha").GetAttributeAsync("src");

                var response = await client.GetAsync($"https://www3.gatineau.ca/servicesenligne/evaluation/{imageSrc}");

                string solvedCaptcha = Process(await response.Content.ReadAsStreamAsync());
                /*
                using (var fs = new FileStream($"./download/{imageSrc.Substring(22)}.jpg", FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }*/

                await page.Locator("#ctl00_cphTexte_tbxListeRue").PressSequentiallyAsync("Bagot, rue");
                await page.Locator("#ctl00_cphTexte_NoImmTextBox").PressSequentiallyAsync("91");
                await page.Locator("#ctl00_cphTexte_CodeTextBox").PressSequentiallyAsync(solvedCaptcha);

                await page.Locator("#ctl00_cphTexte_ListButton").ClickAsync();

               //ClickAsync("#ctl00_cphTexte_ListButton", new() { Force = true });//bp

                await Assertions.Expect(page.Locator("#ctl00_cphTexte_lkbRapport")).ToBeAttachedAsync(new() { Timeout = 10_000 });

                var download = await page.RunAndWaitForDownloadAsync(async () => { await page.Locator("#ctl00_cphTexte_lkbRapport").ClickAsync(); });

                await download.SaveAsAsync("./download/" + download.SuggestedFilename);

                var extractedValues = ProcessPDF("./download/" + download.SuggestedFilename);

                StringBuilder stringBuilder = new StringBuilder();
                foreach (var item in extractedValues)
                {
                    stringBuilder.Append($"{item.Key} = {System.String.Join(", ", item.Value.ToArray())} {Environment.NewLine}");
                }

                return solvedCaptcha + Environment.NewLine + stringBuilder.ToString();
            }
            return "done.";
        }

        private static Dictionary<string, List<string>> ProcessPDF(string pdfFilePath)
        {
            var extractedValues = Constants.KeySettings.Keys.ToDictionary(key => key, key => new List<string>());

            using (PdfDocument document = PdfDocument.Open(pdfFilePath))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToList();

                foreach (var key in Constants.KeySettings.Keys)
                {
                    ExtractPdfValuesByCoordinates(page, words, key, Constants.KeySettings[key], extractedValues);
                }
            }
            return extractedValues;
        }
        private static void ExtractPdfValuesByCoordinates(UglyToad.PdfPig.Content.Page page, List<Word> words, string key, KeyExtractionSettings settings, Dictionary<string, List<string>> values)
        {
            string[] keyParts = key.Split(' '); // Split key into words
            int keyLength = keyParts.Length;
            double yTolerance = 5; // Increased tolerance for slight misalignment

            // Find a sequence of words that match the key
            for (int i = 0; i <= words.Count - keyLength; i++)
            {
                var segment = words.Skip(i).Take(keyLength).ToList();
                if (segment.Count == keyLength && segment.Select(w => w.Text).SequenceEqual(keyParts))
                {
                    // Get bounding box covering the full key phrase
                    double keyLeft = segment.First().BoundingBox.Left;
                    double keyRight = segment.Last().BoundingBox.Right;
                    double keyTop = segment.First().BoundingBox.Top;
                    double keyBottom = segment.Last().BoundingBox.Bottom;
                    double keyMidY = (keyTop + keyBottom) / 2; // Middle Y-coordinate of the key

                    // Find words that are on the same Y level
                    var potentialValues = words
                        .Where(w =>
                            w.BoundingBox.Left > keyRight && // Must be to the right of the key
                            Math.Abs(((w.BoundingBox.Top + w.BoundingBox.Bottom) / 2) - keyMidY) < yTolerance) // similar Y value of the key
                        .OrderBy(w => w.BoundingBox.Left).ToList();//order to keep correct reading order

                    if (settings.UseMaxDistance)
                    {
                        potentialValues = potentialValues.Where(w => (w.BoundingBox.Left - keyRight) < settings.MaxKeyValueDistance).ToList();
                    }
                    // Find the closest word(s) to the right **on nearly the same horizontal level**
                    var valueWords = potentialValues
                        .Take(settings.WordsToTake) // Limit to 5 words (adjustable)
                        .Select(w => w.Text)
                        .ToList();

                    if (valueWords.Any())
                    {
                        values[key].Add(string.Join(" ", valueWords));
                    }
                    //Even if we find a match, we keep going, some elements have duplicates
                }
            }
        }

        public string Process(Stream image)
        {
            ChatClient client = new("gpt-4o", Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

            BinaryData imageBytes = BinaryData.FromStream(image);

            List<ChatMessage> messages =
            [
                new UserChatMessage(
                    ChatMessageContentPart.CreateTextPart("Please provide the numbers and letters in the following image with no additional text."),
                    ChatMessageContentPart.CreateImagePart(imageBytes, "image/png")),
            ];

            ChatCompletion completion = client.CompleteChat(messages);
            return completion.Content[0].Text;
        }
    }
}
