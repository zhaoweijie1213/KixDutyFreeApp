namespace AixDutyFreeCrawler.App.Models.Response
{
    /// <summary>
    /// 
    /// </summary>
    public class PlaceOrderResponse
    {
        /// <summary>
        /// 
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? QueryString { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? Locale { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? OrderID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? OrderToken { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string? ContinueUrl { get; set; }
    }

}
