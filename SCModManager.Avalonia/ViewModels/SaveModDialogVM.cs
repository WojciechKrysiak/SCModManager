using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;

namespace SCModManager.Avalonia.ViewModels
{
    public class SaveModDialogVM : DialogViewModel<Tuple<string, bool>>
    {
        private string _name;
		private bool _mergedFilesOnly;

		public string Name
        {
            get => _name; 
            set =>this.RaiseAndSetIfChanged(ref _name, value);
        }

		public bool MergedFilesOnly
		{
			get => _mergedFilesOnly;
			set => this.RaiseAndSetIfChanged(ref _mergedFilesOnly, value);
		}

		public ICommand Ok { get; }

        public ICommand Cancel { get; }

        readonly Subject<bool> _canSave = new Subject<bool>();

        public SaveModDialogVM(string name)
        {
            Ok = ReactiveCommand.Create(DoSave, this.WhenAny(smd => smd.Name, c => c.Value?.Length > 0));
            Cancel = ReactiveCommand.Create(OnClosing);
            Name = name;
        }

		private void DoSave()
		{
			Result = Tuple.Create(_name, _mergedFilesOnly);
			OnClosing();
		}
    }
}
