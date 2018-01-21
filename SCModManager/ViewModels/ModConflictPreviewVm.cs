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

        public ModConflictPreviewVm(ModConflictDescriptor modConflict)
        {
            _modConflict = modConflict;
        }

        public ModDirectory RootDirectory => _rootDirectory ?? (_rootDirectory = ModDirectory.CreateRoot(_modConflict));

        public ModFileEntry SelectedFile
        {
            get { return _selectedFile; }
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFile, value);
                SelectedModFile = value?.ConflictDescriptor.ConflictingModFiles.FirstOrDefault();
            }
        }

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
            _rootDirectory?.ApplyModFilter(filter);
        }
    }
}
