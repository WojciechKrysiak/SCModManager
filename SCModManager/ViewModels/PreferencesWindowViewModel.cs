using ReactiveUI;
using SCModManager.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private readonly GameConfigurationSection _configurationSection;
        private string _basePath;

        private Subject<bool> _canSave = new Subject<bool>();

        public ICommand SelectBasePath { get; }

        public string BasePath
        {
            get { return _basePath; }
            set
            {
                this.RaiseAndSetIfChanged(ref _basePath, value);
                ValidatePreferences();
            }
        }

        public ICommand Ok { get; }

        public ICommand Cancel { get; }

        public event EventHandler<bool> ShouldClose;

        public ObservableCollection<WhitelistedFileEditableObject> WhiteListedFiles { get; }

        public PreferencesWindowViewModel(GameConfigurationSection configurationSection)
        {
            _configurationSection = configurationSection;
            WhiteListedFiles = new ObservableCollection<WhitelistedFileEditableObject>(configurationSection.WhiteListedFiles.Select(v => new WhitelistedFileEditableObject(v)));
            BasePath = configurationSection.BasePath;
            SelectBasePath = ReactiveCommand.Create(DoSelectBasePath);
            Ok = ReactiveCommand.Create(DoSave, _canSave);
            Cancel = ReactiveCommand.Create(() => ShouldClose?.Invoke(this, false));
            ValidatePreferences();
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
                _configurationSection.WhiteListedFilesConfigSection.Add(file.Value);
            }
            ShouldClose?.Invoke(this, true);
        }

        private void ValidatePreferences()
        {
            var isOk = !string.IsNullOrEmpty(_basePath) &&
                       Directory.Exists(_basePath) &&
                       File.Exists($"{_basePath}\\Settings.txt");
            _canSave.OnNext(isOk);
        }
    }

    public class WhitelistedFileEditableObject : ReactiveObject, IEditableObject
    {
        private string _previousValue;
        private string _value;

        public string Value
        {
            get { return _value; }
            set { this.RaiseAndSetIfChanged(ref _value, value); }
        }

        public void BeginEdit()
        {
            _previousValue = _value;
        }

        public void EndEdit()
        {
        }

        public void CancelEdit()
        {
            Value = _previousValue;
        }

        public WhitelistedFileEditableObject()
        {
            
        }

        public WhitelistedFileEditableObject(string value)
        {
            _previousValue = _value = value;
        }
    }
}
