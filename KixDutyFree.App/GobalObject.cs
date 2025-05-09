using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.App
{
    public static class GobalObject
    {
        /// <summary>
        /// 
        /// </summary>
        public static IServiceProvider? serviceProvider { get; set; } = null;

        public const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36";

    }
}
