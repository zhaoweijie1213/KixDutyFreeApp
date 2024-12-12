using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Models.Entity
{
    [SugarTable("app_config")]
    public class AppConfigEntity
    {
        /// <summary>
        /// 邮箱
        /// </summary>
        [SugarColumn(IsPrimaryKey = true ,IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 重启时可重新下单
        /// </summary>
        public bool ReOrderOnRestart { get; set; }
    }
}
