using Gatineau.Dev.Api.lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using OpenAI.Chat;
using System.Text;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig;
using Gatineau.Dev.Api.Models;
using System.Text.Json;

namespace Gatineau.Dev.Api.Controllers
{
    public class Address
    {
        public string StreetName { get; set;}
        public string StreetNumber { get; set;}
    }
    [Route("v1/api/[controller]")]
    [ApiController]
    public class AssessmentController : ControllerBase
    {
        [HttpGet]
        public async Task<string> Get([FromQuery] Address address)
        {
            using (HttpClient client = new HttpClient())
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new() { SlowMo = 1000 });
                var page = await browser.NewPageAsync();
                await page.GotoAsync("https://www3.gatineau.ca/servicesenligne/evaluation/Adresse.aspx");
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                string imageSrc = await page.GetByAltText("Captcha").GetAttributeAsync("src");

                var response = await client.GetAsync($"https://www3.gatineau.ca/servicesenligne/evaluation/{imageSrc}");

                string solvedCaptcha = Process(await response.Content.ReadAsStreamAsync());

                await page.Locator("#ctl00_cphTexte_tbxListeRue").PressSequentiallyAsync(address?.StreetName);
                await page.Locator("#ctl00_cphTexte_NoImmTextBox").PressSequentiallyAsync(address?.StreetNumber);
                await page.Locator("#ctl00_cphTexte_CodeTextBox").PressSequentiallyAsync(solvedCaptcha);

                await page.Locator("#ctl00_cphTexte_ListButton").ClickAsync();

                await Assertions.Expect(page.Locator("#ctl00_cphTexte_lkbRapport")).ToBeAttachedAsync(new() { Timeout = 10_000 });

                var download = await page.RunAndWaitForDownloadAsync(async () => { await page.Locator("#ctl00_cphTexte_lkbRapport").ClickAsync(); });

                await download.SaveAsAsync("./download/" + download.SuggestedFilename);

                var extractedValues = ProcessPDF("./download/" + download.SuggestedFilename);

                //manually assigning object based on pdf key.
                //I could use an Attribute on each property and reflection to retreive the pdf key
                //or something more complicated like delegates/Actions.. but this is simple and will work
                List<Owner> owners = new List<Owner>();
                for(int i = 0; i < extractedValues["Nom:"].Count; i++)
                {
                    owners.Add(new Owner()
                    {
                        Name = extractedValues["Nom:"][i],
                        SchoolTaxStatus = extractedValues["Statut aux fins d'imposition scolaire:"][i],
                        //This limits multiple owners to an individual address and registration date
                        //I haven't seen any pdf's that have multiple addresses/different dates as of yet.
                        MailingAddress = $"{extractedValues["Adresse postale:"].First()} {extractedValues["Municipalité:"].First()}",
                        RegistrationDate = extractedValues["Date d'inscription au rôle:"].First()
                    }
                    );
                }
                Assessment assessment = new Assessment()
                {
                    FinancialYears = extractedValues["en vigueur pour les exercices financiers:"].First(),
                    Address = extractedValues["Adresse:"].First(),
                    District = extractedValues["Arrondissement:"].First(),
                    LotNumbers = extractedValues["Cadastre(s) et numéro(s)de lot"].First(),
                    RegistrationNumber = extractedValues["Numéro matricule:"].First(),
                    PredominantUse = extractedValues["Utilisation prédominante:"].First(),
                    UnitNumber = extractedValues["Numéro d'unité de voisinage:"].First(),
                    AssessmentFileNumber = extractedValues["Dossier d'évaluation No:"].First(),
                    Owners = owners,
                    Frontage = extractedValues["Mesure frontale:"].First(),
                    Area = extractedValues["Superficie:"].First(),
                    Floors = Int32.Parse(extractedValues["Nombre d'étages:"].First()),
                    YearOfConstruction = Int32.Parse(extractedValues["Année de construction:"].First()),
                    FloorArea = extractedValues["Aire d'étages:"].First(),
                    ConstructionType = extractedValues["Genre de construction:"].First(),
                    PhysicalLink = extractedValues["Lien physique:"].First(),
                    NumberOfDwellings = Int32.Parse(extractedValues["Nombre de logements:"].First()),
                    NumberOfNonResidential = Int32.Parse(extractedValues["Nombre de locaux non residentiels:"].First()),
                    NumberOfRentalRooms = Int32.Parse(extractedValues["Nombre de chambres locatives:"].First()),
                    MarketReferenceDate = extractedValues["Date de référence au marché:"].First(),
                    LandValue = ExtractDecimal(extractedValues["Valeur du terrain:"].First()),
                    BuildingValue = ExtractDecimal(extractedValues["Valeur du bâtiment:"].First()),
                    TotalValue = ExtractDecimal(extractedValues["Valeur de l'immeuble:"].First()),
                    PreviousValue = ExtractDecimal(extractedValues["Valeur de l'immeuble au rôle antérieur:"].First()),
                    TaxRate = extractedValues["d'application des taux variés de taxation :"].First(),
                    TaxableValue = ExtractDecimal(extractedValues["Valeur imposable:"].First()),
                    NonTaxableValue = ExtractDecimal(extractedValues["Valeur non imposable:"].First())
                };
                return JsonSerializer.Serialize(assessment);
            }
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

        private Decimal ExtractDecimal(string s)
        {
            return Convert.ToDecimal(s.Replace(" ", String.Empty).Replace("$", String.Empty));
        }
    }
}
