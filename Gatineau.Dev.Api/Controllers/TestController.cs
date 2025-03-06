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

namespace Gatineau.Dev.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public async Task<string> Get()
        {
            using (HttpClient client = new HttpClient())
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync();
                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://www3.gatineau.ca/servicesenligne/evaluation/Adresse.aspx");
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                string imageSrc = await page.GetByAltText("Captcha").GetAttributeAsync("src");

                var response = await client.GetAsync($"https://www3.gatineau.ca/servicesenligne/evaluation/{imageSrc}");
                string solvedCaptcha = Process(await response.Content.ReadAsStreamAsync());

                using (var fs = new FileStream($"./download/{imageSrc.Substring(22)}.jpg", FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }
                using (var ms = new MemoryStream(await page.ScreenshotAsync()))
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    Image.FromStream(ms).Save("C:/Users/calvi/source/repos/Gatineau.Dev.Api/Gatineau.Dev.Api/download/screenshotBefore.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
#pragma warning restore CA1416 // Validate platform compatibility
                }
                await page.Locator("#ctl00_cphTexte_tbxListeRue").PressSequentiallyAsync("Bagot, rue");
                await page.Locator("#ctl00_cphTexte_NoImmTextBox").PressSequentiallyAsync("88");
                await page.Locator("#ctl00_cphTexte_CodeTextBox").PressSequentiallyAsync(solvedCaptcha);
                using (var ms = new MemoryStream(await page.ScreenshotAsync()))
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    Image.FromStream(ms).Save("C:/Users/calvi/source/repos/Gatineau.Dev.Api/Gatineau.Dev.Api/download/screenshotBeforeClick.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
#pragma warning restore CA1416 // Validate platform compatibility
                }

                var hiddenField = await page.Locator("#ctl00_cphTexte_ScriptManager1_HiddenField").GetAttributeAsync("value");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await page.ClickAsync("#ctl00_cphTexte_ListButton");
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                using (var ms = new MemoryStream(await page.ScreenshotAsync()))
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    Image.FromStream(ms).Save("C:/Users/calvi/source/repos/Gatineau.Dev.Api/Gatineau.Dev.Api/download/screenshotAfter.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
#pragma warning restore CA1416 // Validate platform compatibility
                }

                var onclickString = await page.Locator("#ctl00_cphTexte_lkbRapport").GetAttributeAsync("onclick");

                var download = await page.RunAndWaitForDownloadAsync(async () => { await page.Locator("#ctl00_cphTexte_lkbRapport").ClickAsync(); });

                await download.SaveAsAsync("./download/" + download.SuggestedFilename);

                var text = ProcessPDF("./download/" + download.SuggestedFilename);

                return solvedCaptcha + Environment.NewLine + onclickString + Environment.NewLine + text[FrenchConstants.registrationNumber];
            }
            return "done.";
        }
        public Dictionary<string, string> ProcessPDF(String pdf)
        {
            Dictionary<string, string> extractedData = new Dictionary<string, string>();

            using (PdfDocument document = PdfDocument.Open(pdf))
            {
                //TODO: instead of just getting raw text unformatted, utilize some form of blocking to get us better raw text data: https://github.com/UglyToad/PdfPig/wiki/Document-Layout-Analysis
                //TODO: Once we have an 'ok' raw value,use the Constants in /lib/Constants.cs to extract KVP's 

                var page = document.GetPage(1);
                var words = page.GetWords();
                //double pageHeight = page.Height;
                //double cutoffY = pageHeight * 0.6; // Split at 60% (top 40% for blocks, bottom 60% for words)

                // Process the top 40% using RecursiveXYCut
                /* var blocks = RecursiveXYCut.Instance.GetBlocks(words);
                 foreach (var block in blocks)
                 {
                     if (block.BoundingBox.Bottom >= cutoffY) // Only process top 40%
                     {
                         ExtractPdfKeyValues(block.Text, extractedData);
                     }
                 }*/
                //var words = page.GetWords(NearestNeighbourWordExtractor.Instance);
                //var blocks = DefaultPageSegmenter.Instance.GetBlocks(words);
                //foreach (var block in blocks) {
                //    ExtractPdfKeyValues(block.Text, extractedData);
                //}

            }
            return extractedData;
        }

        public void ExtractPdfKeyValues(string text, Dictionary<string, string> data)
        {
            foreach(var constant in Constants.labelsToRegex)
            {
                ExtractAndStore(text, constant.Value, constant.Key, data);
            }
            //ExtractAndStore(text, ":*(\\S+)\\s" + FrenchConstants.registrationNumber, FrenchConstants.registrationNumber,data);
        }

        public void ExtractAndStore(string text, string pattern, string key, Dictionary<string, string> data)
        {
            Match match = Regex.Match(text, pattern);
            if(match.Success)
            {
                if(match.Value.Contains(key))
                {
                    data[key] = match.Groups[1].Value;
                }
                else
                {
                    data[key] = match.Value;
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
