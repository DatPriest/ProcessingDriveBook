using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProcessingDriveBook;
using System.Globalization;
using System.Text;
using Xamarin.Forms.Maps;

string apikey = @"apikey";


var csvConfig = new CsvConfiguration(CultureInfo.CurrentCulture)
{
    HasHeaderRecord = false,
    Delimiter = ";",
};

Console.WriteLine(File.OpenText("./Resources/LOGBOOK_A_2022.04.05_07.02.csv").CurrentEncoding);
using var streamReader = new StreamReader("./Resources/LOGBOOK_A_2022.04.05_07.02.csv", System.Text.Encoding.UTF8);
//Console.WriteLine(streamReader.ReadToEnd());
using var csvReader = new CsvReader(streamReader, csvConfig);

string value;
bool started = false;
List<OutputDriveData> outputdata = new();
var records = csvReader.GetRecords<DriveData>();
List<DriveData> driveData = new();
List<Batch> batches = new();
foreach (DriveData row in records)
{

    CoordData coord = new CoordData();
    coord.startLatN = ConvertDMSToDecimal(row.StartCoordN);
    coord.startLonE = ConvertDMSToDecimal(row.StartCoordE);
    coord.endLatN = ConvertDMSToDecimal(row.EndCoordN);
    coord.endLonE = ConvertDMSToDecimal(row.EndCoordE);
    var cityData = await MapCoordToStreetsAsync(coord);

    Console.WriteLine(cityData.endCityData.locality);
    OutputDriveData data = new OutputDriveData
    {
        StartKilometer = row.StartKilometer,
        EndDate = row.EndDate,
        StartDate = row.StartDate,
        EndKilometer = row.EndKilometer,
        BusinessDrive = row.BusinessDrive,
        EndTime = row.EndTime,
        StartTime = row.StartTime,
        Nr = row.Nr,
        Comment = row.Comment,
        StartPostalCode = cityData.startCityData.Postal_Code,
        StartStreet = cityData.startCityData.Street == null ? $"{coord.startLatN}, {coord.startLonE}" : $"{cityData.startCityData.Street} {cityData.startCityData.Number}",
        StartCity = cityData.startCityData.locality == null ? $"{coord.startLatN}, {coord.startLonE}" : cityData.startCityData.locality,
        StartName = cityData.startCityData.Name,
        EndStreet = (cityData.endCityData.Street == null || cityData.endCityData.Street == "") ? $"{coord.endLatN}, {coord.endLonE}" : $"{cityData.endCityData.Street} {cityData.endCityData.Number}",
        EndPostalCode = cityData.endCityData.Postal_Code,
        EndCity = cityData.endCityData.locality == null ? $"{coord.endLatN}, {coord.endLonE}" : cityData.endCityData.locality,
        EndName = cityData.endCityData.Name
    };
    Console.WriteLine($"end: {coord.endLatN}, {coord.endLonE}\n start:{coord.endLatN}, {coord.endLonE}");
    Console.WriteLine($"end: {cityData.endCityData.Postal_Code}, {cityData.endCityData.locality}\n start:{cityData.startCityData.Postal_Code}, {cityData.startCityData.locality}");
    Console.WriteLine($"{cityData.startCityData.locality}, {cityData.startCityData.Street}. {cityData.startCityData.Number}, {cityData.endCityData.Street}. {cityData.endCityData.locality}");
    outputdata.Add(data);
    Console.WriteLine($"Loaded data {row.coord}");

    /*
    row.coord = coord;
    driveData.Add(row);
    batches.Add(new Batch()
    {
        query = $"{row.coord.startLatN},{row.coord.startLonE}",
    });
    batches.Add(new Batch()
    {
        query = $"{row.coord.endLatN},{row.coord.endLonE}",
    });*/
    if (outputdata.Count > 20)
        break;
}

var newFile = File.Create($"./Resources/newBook_{DateTime.Now.ToShortDateString()}.csv");
using (var writer = new StreamWriter(newFile))
using (CsvWriter? csvWriter = new(writer, csvConfig))
{
    csvWriter.WriteField("Nr.");
    csvWriter.WriteField("Start Strasse");
    csvWriter.WriteField("Start Stadt");
    csvWriter.WriteField("Start Name");
    csvWriter.WriteField("Start Postleitzahl");
    csvWriter.WriteField("Start Datum");
    csvWriter.WriteField("Start Zeit");
    csvWriter.WriteField("Start Kilometer");
    csvWriter.WriteField("Ende Strasse");
    csvWriter.WriteField("Ende Stadt");
    csvWriter.WriteField("Ende Name");
    csvWriter.WriteField("Ende Postleitzahl");
    csvWriter.WriteField("Ende Datum");
    csvWriter.WriteField("Ende Zeit");
    csvWriter.WriteField("Ende Kilometer");
    csvWriter.WriteField("Dienstfahrt");
    csvWriter.WriteField("Kommentar");
    csvWriter.NextRecord();
    foreach (var data in outputdata)
    {
        csvWriter.WriteField(data.Nr);
        csvWriter.WriteField(data.StartStreet);
        csvWriter.WriteField(data.StartCity);
        csvWriter.WriteField(data.StartName);
        csvWriter.WriteField(data.StartPostalCode);
        csvWriter.WriteField(data.StartDate);
        csvWriter.WriteField(data.StartTime);
        csvWriter.WriteField(data.StartKilometer);
        csvWriter.WriteField(data.EndStreet);
        csvWriter.WriteField(data.EndCity);
        csvWriter.WriteField(data.EndName);
        csvWriter.WriteField(data.EndPostalCode);
        csvWriter.WriteField(data.EndDate);
        csvWriter.WriteField(data.EndTime);
        csvWriter.WriteField(data.EndKilometer);
        csvWriter.WriteField(data.BusinessDrive);
        csvWriter.WriteField(data.Comment);
        csvWriter.NextRecord();
    }
    writer.Flush();
}


double ConvertDMSToDecimal(string dms)
{
    string example = "N 53° 34' 31\"";
    double value;
    double[] values = new double[3];
    //Console.WriteLine(dms);
    string substring = dms.Substring(2, dms.IndexOf("°") - 2);
    values[0] = Convert.ToDouble(substring);

    substring = dms.Substring(dms.IndexOf("°") + 1, dms.IndexOf("'") - dms.IndexOf("°") - 1);
    values[1] = Convert.ToDouble(substring);

    substring = dms.Substring(dms.IndexOf("'") + 1, dms.IndexOf("\"") - dms.IndexOf("'") - 1);
    values[2] = Convert.ToDouble(substring);

    return values[0] + (values[1] / 60) + (values[2] / 3600);
}


async Task<PairCityData?> MapCoordToStreetsAsync(CoordData data)
{
    Geocoder cd = new Geocoder();
    string baseUri =
        "http://api.positionstack.com/v1/reverse?access_key={0}&query={1},{2}&output=json&limit=1";
    string startUri = string.Format(baseUri, apikey, data.startLatN, data.startLonE);
    string endUri = string.Format(baseUri, apikey, data.endLatN, data.endLonE);

    Console.WriteLine(baseUri);
    HttpClient client = new();
    string? jsonString = await client?.GetStringAsync(startUri);
    CityData startCityData = JsonConvert.DeserializeObject<CityData>(JObject.Parse(jsonString)["data"][0].ToString());
    jsonString = await client?.GetStringAsync(endUri);
    CityData endCityData = JsonConvert.DeserializeObject<CityData>(JObject.Parse(jsonString)["data"][0].ToString());

    return new PairCityData(startCityData, endCityData);

}

// Not on the subscription model
async Task<CityData> ProcessBatches(List<Batch> batches)
{
    string baseUri = $"http://api.positionstack.com/v1/forward?access_key={apikey}";
    HttpClient client = new();
    JObject parent = new(new JProperty("batch", (JArray)JToken.FromObject(batches)));


    Console.WriteLine(parent.ToString());
    var content = new StringContent(parent.ToString(), Encoding.UTF8, "application/json");
    HttpResponseMessage response = await client.PostAsync(baseUri, content);
    //response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadAsStringAsync();
    Console.WriteLine(body);
    return null;
}

record class PairCityData
{
    public PairCityData(CityData? startCityData, CityData? endCityData)
    {
        this.startCityData = startCityData;
        this.endCityData = endCityData;
    }

    public CityData startCityData { get; set; }
    public CityData endCityData { get; set; }
}
public record class CoordData
{
    public double startLatN;
    public double startLonE;
    public double endLatN;
    public double endLonE;
}

public record class Batch
{
    public string query;
}
