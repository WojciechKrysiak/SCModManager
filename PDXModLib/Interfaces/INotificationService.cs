using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.Utility
{
    public interface INotificationService
    {
        bool RequestConfirmation(string message, string title);

        void ShowMessage(string message, string title);
    }
}
