namespace KixDutyFree.App.Models.Response
{
    /// <summary>
    /// 保存航班信息
    /// </summary>
    public class FlightSaveInfoResponse
    {
        public string? Action { get; set; }

        public string? QueryString { get; set; }

        public string? Locale { get; set; }

        public FlightSaveInfoData? Data { get; set; }

        public bool AllowCheckout { get; set; }

        public string? Message { get; set; }

        public string? ErrorPlace { get; set; }

        public int Case { get; set; }

        public string? ReservationType { get; set; }

        public List<object>? RestrictedItems { get; set; }

        public List<object>? OfflineProducts { get; set; }

        public string? RedirectUrl { get; set; }
    }

    public class FlightSaveInfoData
    {
        public string? AirlinesNo { get; set; }

        public string? FlightNo { get; set; }

        public string? ConnectingFlight { get; set; }

        public string? DepartureTime { get; set; }

        public string? DepartureDate { get; set; }

        public string? ReservationDate { get; set; }

        public string? ReservationTime { get; set; }

        public int DateDiff { get; set; }

        public int Day { get; set; }

        public string? PickupStoreCode { get; set; }

        public string? MidNightFlight { get; set; }

        public string? Airlines { get; set; }

        public string? AgreeProductLimits { get; set; }

        public string? ProductType { get; set; }

        public string? ReservationType { get; set; }
    }
}
