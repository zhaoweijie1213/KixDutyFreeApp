using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Models.Response
{
    /// <summary>
    /// 登录响应
    /// </summary>
    public class LoginResponse
    {
        /// <summary>
        /// 登录状态
        /// </summary>
        [JsonProperty("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 令牌错误状态
        /// </summary>
        [JsonProperty("csrfError")]
        public bool CsrfError { get; set; }

        /// <summary>
        /// 重定向地址
        /// </summary>
        [JsonProperty("redirectUrl")]
        public string RedirectUrl { get; set; } = string.Empty;


    }
}
