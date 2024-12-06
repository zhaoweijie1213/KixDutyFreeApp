using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Manage
{
    public static class CustomCacheKeys
    {

        /// <summary>
        /// 商品信息
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string ProductInfo(string id)
        {
            return $"ProductInfo_{id}";
        }

        /// <summary>
        /// 商品信息
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string ProductInfoByAddress(string address) 
        {
            return $"ProductInfo_Address_{address}";
        }

    }
}
