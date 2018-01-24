using ReactiveUI;
using SCModManager.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace SCModManager.ViewModels
{
    public class PreferencesWindowViewModel : ReactiveObject
    {
        private GameConfigurationSection _configurationSection;

        public ICommand SelectBasePath { get; }

        public string BasePath
        {
            get { return _configurationSection.BasePath; }
            set { _configurationSection.BasePath = value; }
        }

        public ICommand Ok { get; }

        public ICommand Cancel { get; }

        readonly Subject<bool> _canSave = new Subject<bool>();

        public event EventHandler<bool> ShouldClose;

        public ObservableCollection<string> WhiteListedFiles { get; }

        public PreferencesWindowViewModel(GameConfigurationSection configurationSection)
        {
            _configurationSection = configurationSection;
            WhiteListedFiles = new ObservableCollection<string>(configurationSection.WhiteListedFiles);
            BasePath = configurationSection.BasePath;
            SelectBasePath = ReactiveCommand.Create(DoSelectBasePath);
            Ok = ReactiveCommand.Create(DoSave);
            Cancel = ReactiveCommand.Create(() => ShouldClose?.Invoke(this, false));
        }

        private void DoSelectBasePath()
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                BasePath = dialog.SelectedPath;
            }
        }

        private void DoSave()
        {
            _configurationSection.BasePath = BasePath;
            _configurationSection.WhiteListedFilesConfigSection.Clear();
            foreach(var file in WhiteListedFiles)
            {
                _configurationSection.WhiteListedFilesConfigSection.Add(file);
            }
            ShouldClose?.Invoke(this, true);
        }
    }
}
