using PDXModLib.Interfaces;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Xml;
using System.IO;
using SCModManager.Avalonia.Configuration;
using System.Diagnostics;

namespace SCModManager.Avalonia.Configuration
{
	public class GameConfigurationSection : ConfigurationSection, IGameConfiguration
	{
		private bool _validated;
		private bool _gameDirectoryValid;
		private bool _settingsDirectoryValid;
		private string _gameName;

		public string GameName
		{
			get
			{
				if (_gameName == null)
				{
					_gameName = SectionInformation.SectionName;
				}

				Debug.Assert(!string.IsNullOrWhiteSpace(_gameName));
				return _gameName;
			}
		}

		[ConfigurationProperty("BasePath")]
		public string BasePath
		{
			get { return (string)this["BasePath"]; }
			set { this["BasePath"] = value; }
		}

		[ConfigurationProperty("GameInstallationDirectory")]
		public string GameInstallationDirectory
		{
			get { return (string)this["GameInstallationDirectory"]; }
			set { this["GameInstallationDirectory"] = value; }
		}

		[ConfigurationProperty("UseInstallationDirectory")]
		[DefaultSettingValue("True")]
		public bool UseInstallationDirectory
		{
			get { return (bool)this["UseInstallationDirectory"]; }
			set { this["UseInstallationDirectory"] = value; }
		}

		public string ModsDir => Path.Combine(BasePath,"mod");
		public string SettingsPath => Path.Combine(BasePath, "settings.txt");
		public string BackupPath => Path.Combine(BasePath, "settings.bak");
		public string SavedSelections => Path.Combine(BasePath, "saved_selections.txt");

		public IReadOnlyCollection<string> WhiteListedFiles => WhiteListedFilesConfigSection;

		[ConfigurationProperty("WhiteListedFiles")]
		public WhiteListedFileCollection WhiteListedFilesConfigSection
		{
			get { return (WhiteListedFileCollection)this[WhiteListedFileCollection.WhiteListedFiles]; }
			set { this[WhiteListedFileCollection.WhiteListedFiles] = value; }
		}

		public bool SettingsDirectoryValid
		{
			get
			{
				if (!_validated)
					Validate();
				return _settingsDirectoryValid;
			}
		}

		public bool GameDirectoryValid
		{
			get
			{
				if (!_validated)
					Validate();
				return _gameDirectoryValid || !UseInstallationDirectory;
			}
		}


		public void Init(IDefaultGameConfiguration source)
		{
			_gameName = source.GameName;
			BasePath = source.BasePath;
			GameInstallationDirectory = source.GameInstallationDirectory;
			WhiteListedFilesConfigSection = new WhiteListedFileCollection();
			foreach (var file in source.WhiteListedFiles)
			{
				WhiteListedFilesConfigSection.Add(file);
			}
		}

		public GameConfigurationSection()
		{
		}

		private void Validate()
		{
			_settingsDirectoryValid = File.Exists(SettingsPath);
			_gameDirectoryValid = Directory.Exists(GameInstallationDirectory);
			_validated = true;
		}
	}

	public class WhiteListedFileCollection : ConfigurationElementCollection, ICollection<string>, IReadOnlyCollection<string>
	{
		public const string WhiteListedFiles = "WhiteListedFiles";
		bool ICollection<string>.IsReadOnly => false;

		public void Add(string item)
		{
			this.BaseAdd(new WhiteListedFileElement(item));
		}

		public void Clear()
		{
			this.BaseClear();
		}

		public bool Contains(string item)
		{
			return BaseGetAllKeys().Any(k => (k as string) == item);
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			this.BaseGetAllKeys().OfType<string>().ToList().CopyTo(array, arrayIndex);
		}

		public bool Remove(string item)
		{
			var result = Contains(item);
			BaseRemove(item);
			return result;
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new WhiteListedFileElement();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return (element as WhiteListedFileElement)?.FileName;
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator()
		{
			return BaseGetAllKeys().OfType<string>().GetEnumerator();
		}

		public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;

		protected override string ElementName => WhiteListedFileElement.ElementName;
	}

	public class WhiteListedFileElement : ConfigurationElement
	{
		public const string ElementName = "file";
		private const string PropertyName = "name";

		internal WhiteListedFileElement()
		{
		}

		public WhiteListedFileElement(string fileName)
		{
			FileName = fileName;
		}

		[ConfigurationProperty(PropertyName, IsRequired = true, IsKey = true, DefaultValue = "")]
		public string FileName
		{
			get { return (string)this[PropertyName]; }
			set { this[PropertyName] = value; }
		}
	}
}
