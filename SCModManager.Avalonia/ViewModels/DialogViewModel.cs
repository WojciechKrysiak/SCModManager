using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCModManager.Avalonia.ViewModels
{
    public class DialogViewModel<TResult> : WindowViewModel
    {
		private TResult _result;
		public TResult Result
		{
			get => _result; 
			set => this.RaiseAndSetIfChanged(ref _result, value);
		}

		public event EventHandler Closing; 

		protected DialogViewModel()
		{
		}

		protected void OnClosing()
		{
			Closing?.Invoke(this, EventArgs.Empty);
		}
    }
}
