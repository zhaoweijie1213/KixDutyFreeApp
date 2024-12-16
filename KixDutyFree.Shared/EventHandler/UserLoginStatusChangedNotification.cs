using KixDutyFree.App.Models.Response;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KixDutyFree.Shared.EventHandler
{
    public class UserLoginStatusChangedNotification : INotification
    {
        public string Email { get; } = string.Empty;
        public bool IsLoggedIn { get; }

        public UserLoginStatusChangedNotification(string email, bool isLoggedIn)
        {
            Email = email;
            IsLoggedIn = isLoggedIn;
        }
    }
}
