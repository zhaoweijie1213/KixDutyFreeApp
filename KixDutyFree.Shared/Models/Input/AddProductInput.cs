using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Models.Input
{
    public class AddProductInput
    {
        public string Id { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 购买数量
        /// </summary>
        public int Quantity { get; set; }
    }
}
