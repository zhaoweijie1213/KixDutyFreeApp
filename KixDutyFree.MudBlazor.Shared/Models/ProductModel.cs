using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;

namespace KixDutyFree.App.Models
{
    /// <summary>
    /// 商品
    /// </summary>
    [ExcelImporter(IsLabelingError = true)]
    public class ProductModel
    {
        /// <summary>
        /// 邮箱
        /// </summary>
        [ImporterHeader(Name = "地址")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 数量
        /// </summary>
        [ImporterHeader(Name = "购买数量")]
        public int Quantity { get; set; }
    }
}
