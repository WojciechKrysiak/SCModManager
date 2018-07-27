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
		Task<bool> INotificationService.RequestConfirmation(string message, string title)
		{
            return Task.FromResult(MessageBox.Show(message, title, MessageBoxButton.OKCancel) == MessageBoxResult.OK);
		}

		Task INotificationService.ShowMessage(string message, string title)
		{
			MessageBox.Show(message, title);
			return Task.CompletedTask;
		}
	}
}
