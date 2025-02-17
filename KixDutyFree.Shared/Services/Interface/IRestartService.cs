using QYQ.Base.Common.IOCExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.Services.Interface
{
    public interface IRestartService : ISingletonDependency
    {
        event Action? RestartRequested;

        void RequestRestart();
    }
}
