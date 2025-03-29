namespace Gatineau.Dev.Api.Models
{
    public class Assessment
    {
        public string FinancialYears { get; set; }
        public string Address { get; set; }
        public string District { get; set; }
        public string LotNumbers { get; set; }
        public string RegistrationNumber { get; set; }
        public string PredominantUse { get; set; }
        public string UnitNumber { get; set; }
        public string AssessmentFileNumber { get; set; }
        public List<Owner> Owners { get; set; }
        public string Frontage { get; set; }
        public string Area { get; set; }
        public int Floors { get; set; }
        public int YearOfConstruction { get; set; }
        public string FloorArea { get; set; }
        public string ConstructionType {  get; set; }
        public string PhysicalLink { get; set; }
        public int NumberOfDwellings { get; set; }
        public int NumberOfNonResidential { get; set; }
        public int NumberOfRentalRooms { get; set; }
        public string MarketReferenceDate { get; set; }
        public Decimal LandValue { get; set; }
        public Decimal BuildingValue { get; set; }
        public Decimal TotalValue { get; set; }
        public Decimal PreviousValue { get; set; }
        public string TaxRate { get; set; }
        public Decimal TaxableValue { get; set; }
        public Decimal NonTaxableValue { get; set; }
    }

    public class Owner
    {
        public string Name { get; set; }
        public string SchoolTaxStatus { get; set; }
        public string MailingAddress { get; set; }
        public string RegistrationDate { get; set; }

    }
}
