using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PDXModLib.ModData;
using ReactiveUI;
using SCModManager.DiffMerge;

namespace SCModManager.ViewModels
{
    public class ModConflictPreviewVm : ReactiveObject
    {
        private Func<Mod, bool> _modFilter;
        private readonly ModConflictDescriptor _modConflict;

        private bool _conflictingOnly;
        private ModDirectory _rootDirectory;
        private ModFileEntry _selectedFile;
        private ModFile _selectedModFile;
        private ComparisonContext _comparisonContext;

        public bool ConflictingOnly
        {
            get { return _conflictingOnly; }
            set { this.RaiseAndSetIfChanged(ref _conflictingOnly,value); }
        }

        public ModConflictPreviewVm(ModConflictDescriptor modConflict, Func<Mod, bool> initialModFilter)
        {
            _modFilter = initialModFilter;
            _modConflict = modConflict;
        }

        public ModDirectory RootDirectory => _rootDirectory ?? (_rootDirectory = ModDirectory.CreateRoot(_modConflict, _modFilter));

        public ModFileEntry SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFile, value);
                _filesInOtherMods = null;
                SelectedModFile = FilesInOtherMods?.FirstOrDefault();
                this.RaisePropertyChanged(nameof(FilesInOtherMods));
            }
        }

        private IEnumerable<ModFile> _filesInOtherMods;
        public IEnumerable<ModFile> FilesInOtherMods => _filesInOtherMods ??
                (_filesInOtherMods  = SelectedFile?.ConflictDescriptor.ConflictingModFiles.Where(mf => _modFilter(mf.SourceMod)));

        public ModFile SelectedModFile
        {
            get { return _selectedModFile; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedModFile, value);

                ComparisonContext = value == null
                    ? null
                    : new ComparisonContext(SelectedFile.File, value);
            }
        }

        public ComparisonContext ComparisonContext
        {
            get { return _comparisonContext; }
            set { this.RaiseAndSetIfChanged(ref _comparisonContext, value); }
        }

        public void ApplyModFilter(Func<Mod, bool> filter)
        {
            _modFilter = filter;
            _rootDirectory?.ApplyModFilter(filter);

            if (SelectedModFile != null && !filter(SelectedModFile.SourceMod))
            {
                SelectedModFile = null;
            }

            this.RaisePropertyChanged(nameof(FilesInOtherMods));
        }
    }
}
