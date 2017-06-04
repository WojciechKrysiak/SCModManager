using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SCModManager
{
    class NameConfirmVM : ObservableObject
    {
        private string _name;

        public string Name
        {
            get { return _name; }
            set { Set(ref _name, value);
                Ok.RaiseCanExecuteChanged();
            }
        }

        public RelayCommand Ok { get; }

        public RelayCommand Cancel { get; }

        public NameConfirmVM(string name)
        {
            Ok = new RelayCommand(() => ShouldClose?.Invoke(this, true), () => _name.Length > 0);
            Cancel = new RelayCommand(() => ShouldClose?.Invoke(this, false));
            Name = name;
        }

        public event EventHandler<bool> ShouldClose;
    }
}
