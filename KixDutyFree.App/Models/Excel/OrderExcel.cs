using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;
using OfficeOpenXml.Table;

namespace KixDutyFree.App.Models.Excel
{
    [ExcelExporter(Name = "订单信息", TableStyle = TableStyles.Light10, AutoFitAllColumn = true)]
    public class OrderExcel
    {
        /// <summary>
        /// 账号
        /// </summary>
        [ExporterHeader(DisplayName = "账号")]
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 商品Id
        /// </summary>
        [ExporterHeader(DisplayName = "商品Id")]
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// 订单Id
        /// </summary>
        [ExporterHeader(DisplayName = "订单编号")]
        public string OrderId { get; set; } = string.Empty;



        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
