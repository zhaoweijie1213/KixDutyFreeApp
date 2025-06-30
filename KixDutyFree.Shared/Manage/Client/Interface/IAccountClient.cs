using KixDutyFree.App.Models;
using KixDutyFree.App.Models.Entity;
using KixDutyFree.App.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Manage.Client.Interface
{
    /// <summary>
    /// 公共账号客户端接口，声明登录及后续操作方法。
    /// 两种实现：Selenium 驱动 (AccountClient) 和纯 HTTP (HttpAccountClient)
    /// </summary>
    public interface IAccountClient
    {

        /// <summary>
        /// 账号信息
        /// </summary>
        public AccountInfo Account { get; set; }

        /// <summary>
        /// 是否成功登录
        /// </summary>
        public bool IsLoginSuccess { get; set; }

        /// <summary>
        /// 是否正在下单
        /// </summary>
        public bool IsPlaceOrdering { get; set; }

        /// <summary>
        /// 错误次数
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 初始化并登录，返回是否成功
        /// </summary>
        Task<bool> InitAsync(AccountInfo? account);

        /// <summary>
        /// 退出
        /// </summary>
        /// <returns></returns>
        public Task QuitAsync();

        /// <summary>
        /// 重置客户端
        /// </summary>
        /// <returns></returns>
        public Task<bool> ReloadAsync();

        /// <summary>
        /// 检查登录状态
        /// </summary>
        /// <returns></returns>
        public Task<bool> CheckLoginStatusAsync();

        /// <summary>
        /// 检查商品是否有货
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<bool> CheckProductAvailabilityAsync(ProductInfoEntity product, CancellationToken cancellationToken);

        /// <summary>
        /// 检查下单流程
        /// </summary>
        /// <param name="product"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<(bool, ProductVariationResponse?)> FullCheckProductAvailabilityAsync(ProductInfoEntity product, CancellationToken cancellationToken);
    }
}
