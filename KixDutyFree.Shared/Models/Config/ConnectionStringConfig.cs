using SqlSugar;

namespace KixDutyFree.App.Models.Config
{
    /// <summary>
    /// 连接字符串配置
    /// </summary>
    public class ConnectionStringConfig
    {
        /// <summary>
        /// 主库
        /// </summary>
        public string Master { get; set; } = string.Empty;

        /// <summary>
        /// 从库连接
        /// </summary>
        public List<SlaveConnectionConfig>? SlaveConnections { get; set; }
    }
}
