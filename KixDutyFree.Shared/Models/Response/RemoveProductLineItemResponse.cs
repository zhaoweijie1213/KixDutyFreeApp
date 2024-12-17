using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Models.Response
{
    /// <summary>
    /// 移除购物车商品
    /// </summary>
    public class RemoveProductLineItemResponse
    {
        /// <summary>
        /// 修改的商品Id
        /// </summary>
        public string? UpdatedPid { get; set; }
    }
}
