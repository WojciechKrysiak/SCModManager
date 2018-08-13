using ReactiveUI;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using Avalonia.Threading;
using NLog;
using Avalonia;
using System.Linq;

namespace SCModManager.Avalonia.Ui
{
	public interface DialogResultProvider<TResult>
	{
		TResult Result { get; }
		event EventHandler Close;
	}

	public class DialogWrapper<TVM, TResult>
		where TVM : DialogResultProvider<TResult>
	{
		private TVM _viewModel;
		private ILogger _logger;

		public DialogWrapper(ILogger logger, TVM viewModel)
		{
			_logger = logger;
			_viewModel = viewModel;
		}

		public async Task<TResult> ShowDialog<TV>() where TV : Window, new()
		{
			if (Dispatcher.UIThread.CheckAccess())
			{
				_logger.Debug("Displaying dialog with dispatcher access");
				return await ShowDialogImpl<TV>();
			}

			_logger.Debug("Displaying dialog without dispatcher access");

			TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>();

			await Dispatcher.UIThread.InvokeAsync(async () =>
			{
				_logger.Debug("Invoking ShowDialog on UIThread.");
				var result = await ShowDialogImpl<TV>();
				taskCompletionSource.SetResult(result);
			});

			return await taskCompletionSource.Task;
		}

		private Task<TResult> ShowDialogImpl<TV>() where TV : Window, new()
		{
			var dialog = new TV
			{
				DataContext = _viewModel
			};

			void onClosing(object sender, EventArgs a)
			{
				dialog.Closing -= onClosing;
				_viewModel.Close -= onClosing;
				dialog.Close(_viewModel.Result);
			}

			_viewModel.Close += onClosing;
			dialog.Closing += onClosing;

			return dialog.ShowDialog<TResult>();
		}
	}
}
