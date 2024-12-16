using KixDutyFree.Shared.Services;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.EventHandler
{
    /// <summary>
    /// 用户登录状态通知
    /// </summary>
    /// <param name="accountService"></param>
    public class UserLoginStatusChangedHandler(AccountService accountService) : INotificationHandler<UserLoginStatusChangedNotification>
    {
        public async Task Handle(UserLoginStatusChangedNotification notification, CancellationToken cancellationToken)
        {
            await accountService.UpdateLoginStatusAsync(notification.Email, notification.IsLoggedIn);
        }
    }
}
