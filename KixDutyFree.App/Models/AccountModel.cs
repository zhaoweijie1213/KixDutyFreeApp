using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;

namespace KixDutyFree.App.Models
{
    /// <summary>
    /// 账号信息
    /// </summary>
    [ExcelImporter(IsLabelingError = true)]
    public class AccountModel
    {
        /// <summary>
        /// 邮箱
        /// </summary>
        [ImporterHeader(Name = nameof(Email))]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [ImporterHeader(Name = nameof(Password))]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 航班时间
        /// </summary>
        [ImporterHeader(Name = nameof(Date))]
        public DateTime Date { get; set; }

        /// <summary>
        /// 航空公司
        /// </summary>
        [ImporterHeader(Name = nameof(AirlineName))]
        public string AirlineName { get; set; } = string.Empty;

        /// <summary>
        /// 航班号
        /// </summary>
        [ImporterHeader(Name = nameof(FlightNo))]
        public string FlightNo { get; set; } = string.Empty;

        /// <summary>
        /// 数量
        /// </summary>
        [ImporterHeader(Name = nameof(Quantity))]
        public int Quantity { get; set; }
    }
}
