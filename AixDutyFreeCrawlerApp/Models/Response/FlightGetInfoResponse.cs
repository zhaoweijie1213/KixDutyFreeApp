namespace AixDutyFreeCrawler.App.Models.Response
{
    /// <summary>
    /// 航班信息
    /// </summary>
    public class FlightGetInfoResponse
    {

        public string? Action { get; set; }

        public string? QueryString { get; set; }

        public string? Locale { get; set; }

        public List<FlightData> FlightData { get; set; } = [];
    }

    public class FlightData
    {
        /// <summary>
        /// 
        /// </summary>
        public string? FlightCode { get; set; }

        /// <summary>
        /// 航空公司编号
        /// </summary>
        public string? Flightno1 { get; set; }

        /// <summary>
        /// 航班号
        /// </summary>
        public string? Flightno2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int DepartureDay { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? DepartureTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? PickupStoreCode { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? LateNightFlightFlag { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? AirlineName { get; set; }
    }
}
