using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Playwright;
using OpenAI.Chat;
using System;
using System.IO;
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
using UglyToad.PdfPig.Writer;
using static System.Net.Mime.MediaTypeNames;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using System.Text;
using Microsoft.Extensions.Primitives;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gatineau.Dev.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PdfController : ControllerBase
    {
        [HttpGet]
        public string Get()
        {
            //string filePath = "./download/RapportPDF.pdf";
            string filePath = "C:/Users/calvi/Downloads/RapportPDF.pdf";
            Dictionary<string, KeyExtractionSettings> keySettings = new Dictionary<string, KeyExtractionSettings>()
            {
                { "en vigueur pour les exercices financiers:", new KeyExtractionSettings(false) },
                { "Adresse:", new KeyExtractionSettings(false) },
                { "Arrondissement:", new KeyExtractionSettings(false) },
                { "Cadastre(s) et numéro(s)de lot", new KeyExtractionSettings(false) },
                { "Numéro matricule:", new KeyExtractionSettings(false) },
                { "Utilisation prédominante:", new KeyExtractionSettings(false) },
                { "Numéro d'unité de voisinage:", new KeyExtractionSettings(false) },
                { "Dossier d'évaluation No:", new KeyExtractionSettings(false) },
                { "Nom:", new KeyExtractionSettings(false) },
                { "Statut aux fins d'imposition scolaire:", new KeyExtractionSettings(false) },
                { "Adresse postale:", new KeyExtractionSettings(false) },
                { "Municipalité:", new KeyExtractionSettings(false) },
                { "Date d'inscription au rôle:", new KeyExtractionSettings(true) },
                { "Mesure frontale:", new KeyExtractionSettings(true,200,2) },
                { "Superficie:", new KeyExtractionSettings(true,250,2) },
                { "Nombre d'étages:", new KeyExtractionSettings(false) },
                { "Année de construction:", new KeyExtractionSettings(false) },
                { "Aire d'étages:", new KeyExtractionSettings(false) },
                { "Genre de construction:", new KeyExtractionSettings(false) },
                { "Lien physique:", new KeyExtractionSettings(false) },
                { "Nombre de logements:", new KeyExtractionSettings(false) },
                { "Nombre de locaux non residentiels:", new KeyExtractionSettings(false) },
                { "Nombre de chambres locatives:", new KeyExtractionSettings(false) },
                { "Date de référence au marché:", new KeyExtractionSettings(false) },
                { "Valeur du terrain:", new KeyExtractionSettings(false) },
                { "Valeur du bâtiment:", new KeyExtractionSettings(false) },
                { "Valeur de l'immeuble:", new KeyExtractionSettings(false) },
                { "Valeur de l'immeuble au rôle antérieur:", new KeyExtractionSettings(false) },
                { "d'application des taux variés de taxation :", new KeyExtractionSettings(false) },
                { "Valeur imposable:", new KeyExtractionSettings(true) },
                { "Valeur non imposable:", new KeyExtractionSettings(false) }
            };

            // Store extracted values separately
            var extractedValues = keySettings.Keys.ToDictionary(key => key, key => new List<string>());

            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToList();

                foreach (var key in keySettings.Keys)
                {
                    ExtractValuesByCoordinates(page, words, key, keySettings[key], extractedValues);
                }
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in extractedValues)
            {
                stringBuilder.Append($"{item.Key} = {System.String.Join(", ", item.Value.ToArray())} {Environment.NewLine}");
            }
            return stringBuilder.ToString();
        }
        static void ExtractValuesByCoordinates(UglyToad.PdfPig.Content.Page page, List<Word> words, string key, KeyExtractionSettings settings, Dictionary<string, List<string>> values)
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

        // Class to store extraction settings
        class KeyExtractionSettings
        {
            public bool UseMaxDistance { get; }
            public double MaxKeyValueDistance { get; }

            public int WordsToTake { get; }

            public KeyExtractionSettings(bool useMaxDistance, double maxKeyValueDistance = 100, int wordsToTake = 5)
            {
                UseMaxDistance = useMaxDistance;
                MaxKeyValueDistance = maxKeyValueDistance;
                WordsToTake = wordsToTake;
            }
        }

        private void buildPageExample()
        {
            using (PdfDocument document = PdfDocument.Open("./download/RapportPDF.pdf"))
            {
                var builder = new PdfDocumentBuilder { };
                PdfDocumentBuilder.AddedFont font = builder.AddStandard14Font(Standard14Font.Helvetica);
                var pageBuilder = builder.AddPage(document, 1);
                pageBuilder.SetStrokeColor(0, 255, 0);


                var page = document.GetPage(1);
                var words = page.GetWords();

                foreach (var word in words)
                {
                    if (word.Text == "matricule:")
                    {
                        pageBuilder.DrawRectangle(word.BoundingBox.BottomLeft, word.BoundingBox.Width, word.BoundingBox.Height);
                        pageBuilder.AddText($"l:{word.BoundingBox.BottomLeft.ToString()} r:{word.BoundingBox.TopRight.ToString()}", 8, word.BoundingBox.TopLeft, font);
                    }
                }
                byte[] fileBytes = builder.Build();
                System.IO.File.WriteAllBytes("./download/Processed.pdf", fileBytes);

            }
        }
    }
}
