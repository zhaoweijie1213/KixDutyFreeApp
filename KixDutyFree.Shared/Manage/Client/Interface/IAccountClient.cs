using KixDutyFree.App.Models;
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
        /// 初始化并登录，返回是否成功
        /// </summary>
        Task<bool> InitAsync(AccountInfo account);
    }
}
