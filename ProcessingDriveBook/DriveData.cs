public class DriveData
{
    public string Nr { get; set; }
    public string StartCoordN { get; set; }
    public string StartCoordE { get; set; }
    public string StartDate { get; set; }
    public string StartTime { get; set; }
    public string StartKilometer { get; set; }
    public string EndCoordN { get; set; }
    public string EndCoordE { get; set; }
    public string EndDate { get; set; }
    public string EndTime { get; set; }
    public string EndKilometer { get; set; }
    public string BusinessDrive { get; set; }
    public string Comment { get; set; }

    public CoordData? coord { get; set; }
}