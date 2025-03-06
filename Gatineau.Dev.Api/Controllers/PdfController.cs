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
            string filePath = "./download/RapportPDF.pdf";
            Dictionary<string, List<string>> extractedValues = new Dictionary<string, List<string>>()
            {
                { "en vigueur pour les exercices financiers:", new List<string>() },
                { "Adresse:", new List<string>() },
                { "Arrondissement:", new List<string>() },
                { "Cadastre(s) et numéro(s)de lot", new List<string>() },
                { "Numéro matricule:", new List<string>() },
                { "Utilisation prédominante:", new List<string>() },
                { "Numéro d'unité de voisinage:", new List<string>() },
                { "Dossier d'évaluation No:", new List<string>() },
                { "Nom:", new List<string>() },
                { "Statut aux fins d'imposition scolaire:", new List<string>() },
                { "Adresse postale:", new List<string>() },
                { "Municipalité:", new List<string>() },
                { "Date d'inscription au rôle:", new List<string>() },
                { "Mesure frontale:", new List<string>() },
                { "Superficie:", new List<string>() },
                { "Nombre d'étages:", new List<string>() },
                { "Année de construction:", new List<string>() },
                { "Aire d'étages:", new List<string>() },
                { "Genre de construction:", new List<string>() },
                { "Lien physique:", new List<string>() },
                { "Nombre de logements:", new List<string>() },
                { "Nombre de locaux non residentiels:", new List<string>() },
                { "Date de référence au marché:", new List<string>() },
                { "Nombre de chambres locatives:", new List<string>() },
                { "Valeur du terrain:", new List<string>() },
                { "Valeur du bâtiment:", new List<string>() },
                { "Valeur de l'immeuble:", new List<string>() },
                { "Valeur de l'immeuble au rôle antérieur:", new List<string>() },
                { "d'application des taux variés de taxation :", new List<string>() },
                { "Valeur imposable:", new List<string>() },
                { "Valeur non imposable:", new List<string>() }
            };


            using (PdfDocument document = PdfDocument.Open(filePath))
            {
                var page = document.GetPage(1);
                var words = page.GetWords().ToList();

                foreach (var key in extractedValues.Keys.ToList())
                {
                    ExtractValuesByCoordinates(page, words, key, extractedValues);
                }
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var item in extractedValues)
            {
                stringBuilder.Append($"{item.Key} = {System.String.Join(", ", item.Value.ToArray())} {Environment.NewLine}");
            }
            return stringBuilder.ToString();
        }
        static void ExtractValuesByCoordinates(UglyToad.PdfPig.Content.Page page, List<Word> words, string key, Dictionary<string, List<string>> values)
        {
            string[] keyParts = key.Split(' '); // Split key into words
            int keyLength = keyParts.Length;

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

                    double yTolerance = 5; // Increased tolerance for slight misalignment

                    // Find the closest word(s) to the right **on nearly the same horizontal level**
                    var valueWords = words
                        .Where(w =>
                            w.BoundingBox.Left > keyRight && // Must be to the right
                            Math.Abs(((w.BoundingBox.Top + w.BoundingBox.Bottom) / 2) - keyMidY) < yTolerance) // Increased tolerance
                        .OrderBy(w => w.BoundingBox.Left) // Maintain reading order
                        .Take(5) // Limit to 5 words (adjustable)
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
