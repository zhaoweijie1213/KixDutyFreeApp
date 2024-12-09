using Magicodes.ExporterAndImporter.Core;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Models.Entity
{
    /// <summary>
    /// 账号表
    /// </summary>
    [SugarTable("account")] 
    public class AccountEntity
    {
        /// <summary>
        /// 邮箱
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 航班时间
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 航空公司名称
        /// </summary>
        public string AirlineName { get; set; } = string.Empty;

        /// <summary>
        /// 航班号
        /// </summary>
        public string FlightNo { get; set; } = string.Empty;

        /// <summary>
        /// 默认下单数量
        /// </summary>
        public int Quantity { get; set; }
    }
}
