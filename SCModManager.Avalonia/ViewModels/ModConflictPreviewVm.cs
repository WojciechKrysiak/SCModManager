using PDXModLib.ModData;
using ReactiveUI;
using SCModManager.Avalonia.DiffMerge;
using SCModManager.Avalonia.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace SCModManager.Avalonia.ViewModels
{
	public class ModConflictPreviewVm : ReactiveObject
    {
        private Func<Mod, bool> _modFilter;
        private readonly ModConflictDescriptor _modConflict;

		private ISubject<Unit> _newFilter;

        private bool _conflictingOnly;
        private ModDirectory _rootDirectory;
        private ModFileEntry _selectedFile;
        private ModFile _selectedModFile;
        private ObservableAsPropertyHelper<ComparisonContext> _comparisonContext;

        public bool ConflictingOnly
        {
            get { return _conflictingOnly; }
            set { this.RaiseAndSetIfChanged(ref _conflictingOnly,value); }
        }

        public ModConflictPreviewVm(ModConflictDescriptor modConflict, Func<Mod, bool> initialModFilter)
        {
            _modFilter = initialModFilter;
            _modConflict = modConflict;
			_newFilter = new Subject<Unit>();

			FilesInOtherMods = this.ObservableForProperty(x => x.SelectedFile)
				                   .Select(change => change.Value?.ConflictDescriptor.ConflictingModFiles.Where(mf => _modFilter(mf.SourceMod)))
								   .ToReactiveCollection(x => x, null, null, _newFilter);
			_comparisonContext = this.ObservableForProperty(x => x.SelectedModFile).Value().Select(v => v == null ? null : new ComparisonContext(SelectedFile.File, v)).ToProperty(this, x => x.ComparisonContext);
		}

        public ModDirectory RootDirectory => _rootDirectory ?? (_rootDirectory = ModDirectory.CreateRoot(_modConflict, _modFilter));

        public ModFileEntry SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFile, value);
            }
        }

		public IEnumerable<ModFile> FilesInOtherMods { get; }

        public ModFile SelectedModFile
        {
            get { return _selectedModFile; }
            set
			{
				this.RaiseAndSetIfChanged(ref _selectedModFile, value);
			}
        }

        public ComparisonContext ComparisonContext
        {
            get { return _comparisonContext.Value; }
        }

        public void ApplyModFilter(Func<Mod, bool> filter)
        {
            _modFilter = filter;
            _rootDirectory?.ApplyModFilter(filter);

            if (SelectedModFile != null && !filter(SelectedModFile.SourceMod))
            {
                SelectedModFile = null;
            }

			_newFilter.OnNext(Unit.Default);
        }
    }
}
