using System.Text.RegularExpressions;

namespace Gatineau.Dev.Api.lib
{

    public static class Constants
    {
        public static Dictionary<string, KeyExtractionSettings> KeySettings = new Dictionary<string, KeyExtractionSettings>()
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


    }

}
