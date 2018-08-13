using ReactiveUI;
using SCModManager.Avalonia.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using System.Windows.Input;
using Avalonia.Controls;
using PDXModLib.Interfaces;
using SCModManager.Avalonia.Configuration;
using SCModManager.Avalonia.ViewModels;

namespace SCModManager.Avalonia.ViewModels
{
    public class PreferencesWindowViewModel : DialogViewModel<bool>
    {
        private readonly GameConfigurationSection _configurationSection;
		private readonly IConfigurationService configurationService;
		private IDisposable _newFileEditedDisposable;

        private IObservable<bool> _canSave;

        public ICommand SelectBasePath { get; }

        public string BasePath
        {
            get { return _configurationSection.BasePath; }
            set
            {
				if (_configurationSection.BasePath != value)
				{
					_configurationSection.BasePath = value;
					this.RaisePropertyChanged(nameof(BasePath));
				}
            }
        }

		public string InstallationPath
		{
			get { return _configurationSection.GameInstallationDirectory; }
			set
			{
				if (_configurationSection.GameInstallationDirectory != value)
				{
					_configurationSection.GameInstallationDirectory = value;
					this.RaisePropertyChanged(nameof(InstallationPath));
				}
			}
		}

		public ICommand Ok { get; }

        public ICommand Cancel { get; }

		public ICommand DeleteItem { get; }

        public ReactiveList<WhitelistedFileEditableObject> WhiteListedFiles { get; }

		public PreferencesWindowViewModel(IGameConfiguration configurationSection, IConfigurationService configurationService)
		{
			_configurationSection = new GameConfigurationSection();
			_configurationSection.Init(configurationSection);
			WhiteListedFiles = new ReactiveList<WhitelistedFileEditableObject>()
			{
				ChangeTrackingEnabled = true 
			};

			this._canSave = this.WhenAny(x => x.BasePath, c => IsPathValid(c.Value)).CombineLatest(
				   WhiteListedFiles.ItemChanged.Where(i => i.PropertyName == nameof(WhitelistedFileEditableObject.IsValid)).Select(s => s.Sender.IsValid),
				   (c1, c2) => c1 && c2
				   );

			WhiteListedFiles.AddRange(configurationSection.WhiteListedFiles.Select(v => new WhitelistedFileEditableObject(v)));

			var newFile = new WhitelistedFileEditableObject { IsNew = true };
			_newFileEditedDisposable = newFile.WhenAny(t => t.Value, x => x).Subscribe(NewFileEdited);
			WhiteListedFiles.Add(newFile);
            SelectBasePath = ReactiveCommand.Create(DoSelectBasePath);
            Ok = ReactiveCommand.Create(DoSave, _canSave);
            Cancel = ReactiveCommand.Create(() => OnClosing());
			DeleteItem = ReactiveCommand.Create<WhitelistedFileEditableObject>(DoDelete);
			this.configurationService = configurationService;
		}

		async void DoSelectBasePath()
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync();
            if (!string.IsNullOrEmpty(result))
            { 
                BasePath = result;
            }
        }

        private void DoSave()
        {
            _configurationSection.BasePath = BasePath;
            _configurationSection.WhiteListedFilesConfigSection.Clear();
            foreach(var file in WhiteListedFiles.Where(v => !string.IsNullOrWhiteSpace(v.Value)))
            {
                _configurationSection.WhiteListedFilesConfigSection.Add(file.Value);
            }
			configurationService.SaveConfiguration(_configurationSection);
			Result = true;
			OnClosing();
        }

		private void DoDelete(WhitelistedFileEditableObject obj)
		{
			WhiteListedFiles.Remove(obj);
		}

		public void NewFileEdited(IObservedChange<WhitelistedFileEditableObject, string> obj)
		{
			if (!string.IsNullOrEmpty(obj.Value))
			{
				obj.Sender.IsNew = false;
				var newFile = new WhitelistedFileEditableObject { IsNew = true };
				_newFileEditedDisposable.Dispose();
				_newFileEditedDisposable = newFile.WhenAny(t => t.Value, x => x).Subscribe(NewFileEdited);
				WhiteListedFiles.Add(newFile);
			}
		}

		private bool IsPathValid(string path)
		{
			return !string.IsNullOrEmpty(path) &&
					  Directory.Exists(path) &&
					  File.Exists($"{path}\\Settings.txt");
		}
    }

    public class WhitelistedFileEditableObject : ReactiveObject
    {
        private string _value;
		private ObservableAsPropertyHelper<bool> _isValid;
		private bool _isNew;

		public string Value
        {
            get { return _value; }
            set { this.RaiseAndSetIfChanged(ref _value, value); }
        }

		public bool IsNew
		{
			get { return _isNew; }
			set { this.RaiseAndSetIfChanged(ref _isNew, value); }
		}

		public bool IsValid => _isValid.Value;


        public WhitelistedFileEditableObject()
        {
			_isValid = this.WhenAny(x => x.Value, x => x.IsNew, (v, n) => n.Value || !string.IsNullOrEmpty(v.Value)).ToProperty(this, x => x.IsValid, true);
        }

		public WhitelistedFileEditableObject(string value)
			: this()
        {
            Value = value;
        }
    }
}
