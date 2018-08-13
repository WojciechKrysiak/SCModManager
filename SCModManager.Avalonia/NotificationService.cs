using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using NLog;
using PDXModLib.Utility;
using SCModManager.Avalonia.Ui;
using SCModManager.Avalonia.Utility;
using SCModManager.Avalonia.ViewModels;
using SCModManager.Avalonia.Views;

namespace SCModManager.Avalonia
{
    internal class NotificationService : INotificationService
    {
		private ILogger _logger;
		private readonly IShowDialog<NotificationViewModel, DialogResult, string, string, ButtonTypes, NotificationType> showWindow;

		public NotificationService(ILogger logger, IShowDialog<NotificationViewModel, DialogResult, string, string, ButtonTypes, NotificationType> showWindow )
		{
			_logger = logger;
			this.showWindow = showWindow;
		}

        public async Task<bool> RequestConfirmation(string message, string title)
        {
			return await showWindow.Show(message, title, ButtonTypes.YesNo, NotificationType.Question) == DialogResult.Yes;
		}

		public async Task ShowMessage(string message, string title)
        {
			await showWindow.Show(message, title, ButtonTypes.Ok, NotificationType.Information);
		}
    }
}
