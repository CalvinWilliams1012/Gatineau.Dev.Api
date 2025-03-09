namespace Gatineau.Dev.Api.lib
{
    public class KeyExtractionSettings
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
}
