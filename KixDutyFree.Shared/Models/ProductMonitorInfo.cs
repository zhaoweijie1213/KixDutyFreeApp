using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Models
{
    /// <summary>
    /// 商品监控信息
    /// </summary>
    public class ProductMonitorInfo
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 图片
        /// </summary>
        public string Image { get; set; } = string.Empty;

        /// <summary>
        /// 设置的购买数量
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// 最大购买数量
        /// </summary>
        public int MaxQuantity { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 监控状态
        /// </summary>
        public bool MonitorStatus {  get; set; }

        /// <summary>
        /// 是否可以购买
        /// </summary>
        public bool IsAvailable { get; set; }
    }
}
