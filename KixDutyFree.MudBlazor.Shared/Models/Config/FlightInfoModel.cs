namespace KixDutyFree.App.Models.Config
{
    public class FlightInfoModel
    {
        public DateTime Date { get; set; }

        public string AirlineName { get; set; } = string.Empty;

        public string FlightNo { get; set; } = string.Empty;
    }
}
