using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using OfficeOpenXml.Table;

namespace KixDutyFree.App.Models.Excel
{
    [ExcelExporter(Name = "订单信息", TableStyle = TableStyles.Light10, AutoFitAllColumn = true)]
    public class OrderExcel
    {

        /// <summary>
        /// 订单Id
        /// </summary>
        [ImporterHeader(Name = "订单编号")]
        [ExporterHeader(DisplayName = "订单编号", AutoCenterColumn = true)]
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// 账号
        /// </summary>
        [ImporterHeader(Name = "账号")]
        [ExporterHeader(DisplayName = "账号", AutoCenterColumn = true)]
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 商品Id
        /// </summary>
        [ImporterHeader(Name = "商品Id")]
        [ExporterHeader(DisplayName = "商品Id", AutoCenterColumn = true)]
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// 商品Id
        /// </summary>
        [ImporterHeader(Name = "商品名称")]
        [ExporterHeader(DisplayName = "商品名称", AutoCenterColumn = true)]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 航班时间
        /// </summary>
        [ImporterHeader(Name = "航班时间")]
        [ExporterHeader(DisplayName = "航班时间", AutoCenterColumn = true, Format = "yyyy-MM-dd HH:mm")]
        public DateTime FlightDate { get; set; }

        /// <summary>
        /// 航空公司
        /// </summary>
        [ImporterHeader(Name = "航空公司")]
        [ExporterHeader(DisplayName = "航空公司", AutoCenterColumn = true)]
        public string AirlineName { get; set; } = string.Empty;

        /// <summary>
        /// 航班号
        /// </summary>
        [ImporterHeader(Name = "航班号")]
        [ExporterHeader(DisplayName = "航班号", AutoCenterColumn = true)]
        public string FlightNo { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        [ImporterHeader(Name = "创建时间")]
        [ExporterHeader(DisplayName = "创建时间", AutoCenterColumn = true, Format = "yyyy-MM-dd HH:mm:ss")]
        public DateTime CreateTime { get; set; }
    }
}
