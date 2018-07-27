using ReactiveUI;
using SCModManager.Avalonia.Ui;
using SCModManager.Ui.FontAwesome;
using System;
using System.Collections.Generic;
using System.Text;
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
		Ok,
		Cancel,
		Yes,
		No
	}


	public class NotificationViewModel : ReactiveObject, DialogResultProvider<DialogResult>
	{
		private readonly ButtonTypes buttons;
		private bool isFocused;

		public event EventHandler Close;

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

		public DialogResult DefaultResult => ShowCancel ? DialogResult.Cancel : DialogResult.No;

		public bool ShowOk => buttons == ButtonTypes.Ok || buttons == ButtonTypes.OkCancel;
		public bool ShowCancel => buttons == ButtonTypes.OkCancel;
		public bool ShowYes => buttons == ButtonTypes.YesNo;
		public bool ShowNo => buttons == ButtonTypes.YesNo;

		public bool IsInfo => Type == NotificationType.Information;
		public bool IsQuestion => Type == NotificationType.Question;
		public bool IsWarning => Type == NotificationType.Warning;
		public bool IsError => Type == NotificationType.Error;

		public DialogResult Result { get; private set; }

		private void DoOk()
		{
			Result = ShowOk ? DialogResult.Ok : DialogResult.Yes;
			Close?.Invoke(this, EventArgs.Empty);
		}

		private void DoCancel()
		{
			Result = ShowCancel ? DialogResult.Cancel : DialogResult.No;
			Close?.Invoke(this, EventArgs.Empty);
		}


	}
}
