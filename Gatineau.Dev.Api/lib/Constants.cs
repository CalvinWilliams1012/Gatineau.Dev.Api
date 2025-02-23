namespace Gatineau.Dev.Api.lib
{

    public static class Constants
    {
        public static Dictionary<String, String> labelToRegex = new Dictionary<string, string>(){ 
            { FrenchConstants.financialYears, RegexConstants.lookBehind },
            { FrenchConstants.registrationNumber, RegexConstants.lookBehind },
            { FrenchConstants.predominantUse, RegexConstants.lookBehind },
            { FrenchConstants.neighborhoodUnitNumber, RegexConstants.lookBehind },
            { FrenchConstants.address, RegexConstants.lookBehind },
            { FrenchConstants.borough, RegexConstants.lookBehind },
            { FrenchConstants.landRegistryLotNumber, RegexConstants.lookBehind },
            { FrenchConstants.assessmentFileNum, RegexConstants.lookBehind },
            { FrenchConstants.name, RegexConstants.lookBehind },
            { FrenchConstants.mailingAddress, RegexConstants.lookBehind },
            { FrenchConstants.municipality, RegexConstants.lookBehind },
            { FrenchConstants.dateOfRegistration, RegexConstants.lookBehind },
            { FrenchConstants.complement, RegexConstants.lookBehind },
            { FrenchConstants.schoolTaxStatus, RegexConstants.lookBehind },
            { FrenchConstants.numberOfFloors, RegexConstants.lookBehind },
            { FrenchConstants.yearOfConstruction, RegexConstants.lookBehind },
            { FrenchConstants.floorArea, RegexConstants.lookBehind },
            { FrenchConstants.typeOfConstruction, RegexConstants.lookBehind },
            { FrenchConstants.physicalLink, RegexConstants.lookBehind },
            { FrenchConstants.numberOfDwellings, RegexConstants.lookBehind },
            { FrenchConstants.numberOfNonResPremises, RegexConstants.lookBehind },
            { FrenchConstants.numberOfRentalRooms, RegexConstants.lookBehind },
            { FrenchConstants.area, RegexConstants.lookBehind },
            { FrenchConstants.frontage, RegexConstants.lookBehind },
            { FrenchConstants.marketReferenceDate, RegexConstants.lookBehind },
            { FrenchConstants.landValue, RegexConstants.lookBehind },
            { FrenchConstants.buildingValue, RegexConstants.lookBehind },
            { FrenchConstants.propertyValue, RegexConstants.lookBehind },
            { FrenchConstants.previousPropertyValue, RegexConstants.lookBehind },
            { FrenchConstants.taxCategory, RegexConstants.lookBehind },
            { FrenchConstants.taxValue, RegexConstants.lookBehind },
            { FrenchConstants.nonTaxValue, RegexConstants.lookBehind },
            { FrenchConstants.municipalityOf, RegexConstants.lookBehind }
        };
    }
    public static class FrenchConstants
    {
        public static string financialYears = "en vigueur pour les exercices financiers";
        public static string registrationNumber = "Numéro matricule";
        public static string predominantUse = "Utilisation prédominante";
        public static string neighborhoodUnitNumber = "Numéro d'unité de voisinage";
        public static string address = "Adresse";
        public static string borough = "Arrondissement";
        public static string landRegistryLotNumber = "Cadastre(s) et numéro(s)de lot";
        public static string assessmentFileNum = "Dossier d'évaluation No";
        public static string name = "Nom";
        public static string mailingAddress = "Adresse postale";
        public static string municipality = "Municipalité";
        public static string dateOfRegistration = "Date d'inscription au rôle";
        public static string complement = "Complément";
        public static string schoolTaxStatus = "Statut aux fins d'imposition scolaire";
        public static string numberOfFloors = "Nombre d'étages";
        public static string yearOfConstruction = "Année de construction";
        public static string floorArea = "Aire d'étages";
        public static string typeOfConstruction = "Genre de construction";
        public static string physicalLink = "Lien physique";
        public static string numberOfDwellings = "Nombre de logements";
        public static string numberOfNonResPremises = "Nombre de locaux non residentiels";
        public static string numberOfRentalRooms = "Nombre de chambres locatives";
        public static string area = "Superficie";
        public static string frontage = "Mesure frontale";
        public static string marketReferenceDate = "Date de référence au marché";
        public static string landValue = "Valeur du terrain";
        public static string buildingValue = "Valeur du bâtiment";
        public static string propertyValue = "Valeur de l'immeuble";
        public static string previousPropertyValue = "Valeur de l'immeuble au rôle antérieur";
        public static string taxCategory = "Catégorie et classe d'immeuble à des fins d'application des taux variés de taxation";
        public static string taxValue = "Valeur imposable";
        public static string nonTaxValue = "Valeur non imposable";
        public static string municipalityOf = "Municipalité de";
    }

    public static class RegexConstants
    {
        public static string lookBehind = ":*(\\S+)\\s";
    }
}
