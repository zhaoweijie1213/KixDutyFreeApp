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
        
        /// <summary>
        /// 商品监控
        /// </summary>
        /// <param name="email"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public static string ProductMonitor(string email,string productId) 
        {
            return $"ProductMonitor_{email}_{productId}";
        }

    }
}
