using KixDutyFree.App.Models.Entity;
using Magicodes.ExporterAndImporter.Core;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Models.Entity
{
    [SugarTable("orders")]
    public class OrdersEntity
    {
        /// <summary>
        /// 订单Id
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// 账号
        /// </summary>
        public string Account { get; set; } = string.Empty;

        /// <summary>
        /// 商品Id
        /// </summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// 商品信息
        /// </summary>
        [Navigate(NavigateType.OneToOne, nameof(ProductId))]
        public ProductInfoEntity? Product { get; set; }

        /// <summary>
        /// 航班时间
        /// </summary>
        public DateTime FlightDate { get; set; }

        /// <summary>
        /// 航空公司
        /// </summary>
        public string AirlineName { get; set; } = string.Empty;

        /// <summary>
        /// 航班号
        /// </summary>
        public string FlightNo { get; set; } = string.Empty;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}
