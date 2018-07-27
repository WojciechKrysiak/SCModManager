using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.Utility
{
    public interface INotificationService
    {
        Task<bool> RequestConfirmation(string message, string title);

        Task ShowMessage(string message, string title);
    }
}
