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
    public class NameConfirmVM : DialogViewModel<string>
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value);
				  this.Result = value;
                _canSave.OnNext(_name.Length > 0);
            }
        }

        public ICommand Ok { get; }

        public ICommand Cancel { get; }

        readonly Subject<bool> _canSave = new Subject<bool>();

        public NameConfirmVM(string name)
        {
            Ok = ReactiveCommand.Create(OnClosing, _canSave);
            Cancel = ReactiveCommand.Create(() => {
				Result = null;
				OnClosing();
			});
            Name = name;
        }

    }
}
