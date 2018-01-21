using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PDXModLib.Utility;

namespace SCModManager
{
    internal class NotificationService : INotificationService
    {
        public bool RequestConfirmation(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.OKCancel) == MessageBoxResult.OK;
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title);
        }
    }
}
