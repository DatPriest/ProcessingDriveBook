using Newtonsoft.Json;

namespace ProcessingDriveBook
{
    public class CityData
    {

        [JsonProperty]
        public string Street { get; set; }

        [JsonProperty]
        public string locality { get; set; }

        [JsonProperty]
        public string Postal_Code { get; set; }

        [JsonProperty]
        public string Number { get; set; }

        [JsonProperty]
        public string Name { get; set; }
    }
}
