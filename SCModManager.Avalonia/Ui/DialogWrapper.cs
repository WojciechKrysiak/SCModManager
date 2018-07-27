using ReactiveUI;
using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

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

		public DialogWrapper(TVM viewModel)
		{
			_viewModel = viewModel;
		}

		public Task<TResult> ShowDialog<TV>() where TV : Window, new()
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
