namespace AixDutyFreeCrawler.App.Models
{
    public class ProductMonitorInfo
    {
        /// <summary>
        /// 
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Name { get; set; } =string.Empty;

        /// <summary>
        /// 
        /// </summary>
        public OrderSetup Setup {  get; set; }
    }

    public enum OrderSetup
    {

    }
}
