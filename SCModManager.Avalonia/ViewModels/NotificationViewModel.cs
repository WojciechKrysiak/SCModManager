using ReactiveUI;
using SCModManager.Avalonia.Ui;
using System;
using System.Windows.Input;

namespace SCModManager.Avalonia.ViewModels
{
	public enum NotificationType
	{
		Question,
		Information,
		Warning,
		Error
	}

	public enum ButtonTypes
	{
		Ok,
		OkCancel,
		YesNo
	}

	public enum DialogResult
	{
		Cancel = 0,
		Ok = 1,
		No = 0,
		Yes = 1,
	}


	public class NotificationViewModel : DialogViewModel<DialogResult>
	{
		private readonly ButtonTypes buttons;
		private bool isFocused;

		public delegate NotificationViewModel Factory(string message, string title, ButtonTypes buttons, NotificationType type);

		public NotificationViewModel(string message, string title = "Notification", ButtonTypes buttons = ButtonTypes.Ok, NotificationType type = NotificationType.Information)
		{
			Message = message;
			Title = title;
			Type = type;
			this.buttons = buttons;

			Result = DialogResult.Cancel;

			Ok = ReactiveCommand.Create(DoOk);
			Cancel = ReactiveCommand.Create(DoCancel);
		}

		public bool IsFocused
		{
			get => isFocused;
			set => this.RaiseAndSetIfChanged(ref isFocused, value);
		}

		public ICommand Ok { get; set; }
		public ICommand Cancel { get; set; }

		public string Message { get; set; }
		public string Title { get; set; }
		public NotificationType Type { get; }

		public bool ShowOk => buttons == ButtonTypes.Ok || buttons == ButtonTypes.OkCancel;
		public bool ShowCancel => buttons == ButtonTypes.OkCancel;
		public bool ShowYes => buttons == ButtonTypes.YesNo;
		public bool ShowNo => buttons == ButtonTypes.YesNo;

		public bool IsInfo => Type == NotificationType.Information;
		public bool IsQuestion => Type == NotificationType.Question;
		public bool IsWarning => Type == NotificationType.Warning;
		public bool IsError => Type == NotificationType.Error;

		private void DoOk()
		{
			Result = DialogResult.Ok;
			OnClosing();
		}

		private void DoCancel()
		{
			Result = DialogResult.Cancel;
			OnClosing();
		}
	}
}
