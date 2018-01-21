using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;

namespace SCModManager
{
    class NameConfirmVM : ReactiveObject
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value);
                _canSave.OnNext(_name.Length > 0);
            }
        }

        public ICommand Ok { get; }

        public ICommand Cancel { get; }

        readonly Subject<bool> _canSave = new Subject<bool>();

        public NameConfirmVM(string name)
        {
            Ok = ReactiveCommand.Create(() => ShouldClose?.Invoke(this, true), _canSave);
            Cancel = ReactiveCommand.Create(() => ShouldClose?.Invoke(this, false));
            Name = name;
        }

        public event EventHandler<bool> ShouldClose;
    }
}
