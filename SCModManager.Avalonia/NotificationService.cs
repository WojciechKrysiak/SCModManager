using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PDXModLib.Utility;
using SCModManager.Avalonia.Ui;
using SCModManager.Avalonia.ViewModels;
using SCModManager.Avalonia.Views;

namespace SCModManager
{
    internal class NotificationService : INotificationService
    {
        public async Task<bool> RequestConfirmation(string message, string title)
        {
			return await new DialogWrapper<NotificationViewModel, DialogResult>(new NotificationViewModel(message, title, ButtonTypes.YesNo)).ShowDialog<NotificationView>() == DialogResult.Yes;
		}

		public async Task ShowMessage(string message, string title)
        {
			await new DialogWrapper<NotificationViewModel, DialogResult>(new NotificationViewModel(message, title)).ShowDialog<NotificationView>();
		}
    }
}
