using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.FSharp.Core;
using PDXModLib.Interfaces;
using PDXModLib.ModData;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace SCModManager.ViewModels
{
    public abstract class ModFileHolder : ReactiveObject
    {
        public string Filename { get; protected set; }

        public abstract bool HasConflicts { get; }

        protected ModFileHolder(string name)
        {
            Filename = name;
        }

        public abstract void ApplyModFilter(Func<Mod, bool> filter);
    }

    public class ModDirectory : ModFileHolder
    {
        private readonly int _level;
        private static readonly char[] Separators = { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

        private Func<Mod, bool> _modFilter;
        private IEnumerable<ModFileConflictDescriptor> _source;
        private IEnumerable<ModDirectory> _directories;
        private IEnumerable<ModFileEntry> _files;
        private ObservableCollection<ModFileHolder> _contents;
        private bool _hasConflicts;

        public IEnumerable<ModFileHolder> Files => _contents ?? (_contents = CreateChildren());

        public override bool HasConflicts => _hasConflicts;

        private ModDirectory(string name, int level, IEnumerable<ModFileConflictDescriptor> source, Func<Mod, bool> initialModFilter)
            : base(name)
        {
            _level = level;
            _modFilter = initialModFilter;
            _source = source.ToList();
            Filename = name;
            _hasConflicts = _source.Any(mfcd => mfcd.ConflictingModFiles.Any());
        }

        public static ModDirectory CreateRoot(ModConflictDescriptor conflictDescriptor, Func<Mod, bool> initialModFilter = null)
        {
            initialModFilter = initialModFilter ?? (m => true);
            return new ModDirectory(string.Empty, 0, conflictDescriptor.FileConflicts, initialModFilter);
        }

        public override void ApplyModFilter(Func<Mod, bool> filter)
        {
            var conflcits = false;
            _modFilter = filter;
            if (_contents != null)
            {
                foreach (var modFileHolder in _contents)
                {
                    modFileHolder.ApplyModFilter(filter);
                }
                conflcits = _contents.Any(f => f.HasConflicts);
            }
            else 
                conflcits = _hasConflicts = _source.Any(mfcd => mfcd.ConflictingModFiles.Any(mf => filter(mf.SourceMod)));

            this.RaiseAndSetIfChanged(ref _hasConflicts, conflcits, nameof(HasConflicts));
        }

        public void UpdateDirectoryContents(string directoryName, IEnumerable<ModFileConflictDescriptor> conflicts)
        {
            var currentLevel = directoryName.Split(Separators, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (currentLevel == null)
            {
                _source = conflicts;
                var filesAtThisLevel = _source.Select(m => Tuple.Create(m.File.Path.Split(Separators), m)).ToList();

                var filesToRemove = _files.Where(f => filesAtThisLevel.Any(t => t.Item2.File == f.File));

                foreach (var file in _files)
                {
                    _contents.Remove(file);
                }

                _files = CreateFiles(filesAtThisLevel.Where(t => t.Item1.Length == _level + 1));
                foreach (var file in _files)
                {
                    _contents.Add(file);
                }
            }
            else
            {
                var nextLevel = directoryName.Substring(directoryName.IndexOf(currentLevel) + currentLevel.Length);

                var subDir = _directories
                    .First(md => string.CompareOrdinal(md.Filename, currentLevel) == 0);

                subDir.UpdateDirectoryContents(nextLevel, conflicts);
            }

            this.RaiseAndSetIfChanged(ref _hasConflicts, _contents.Any(f => f.HasConflicts), nameof(HasConflicts));
        }

        private ObservableCollection<ModFileHolder> CreateChildren()
        {
            var kids = _source.Select(m => Tuple.Create(m.File.Path.Split(Separators), m)).ToList();

            _directories = CreateDirectories(kids.Where(t => t.Item1.Length > _level + 1));

            _files = CreateFiles(kids.Where(t => t.Item1.Length == _level + 1));

            return new ObservableCollection<ModFileHolder>(_directories.OfType<ModFileHolder>().Concat(_files));
        }

        private IEnumerable<ModFileEntry> CreateFiles(IEnumerable<Tuple<string[], ModFileConflictDescriptor>> fileConflicts)
        {
            return fileConflicts.Select(fc => new ModFileEntry(fc.Item1.Last(), fc.Item2, _modFilter)).OrderBy(mfe => mfe.Filename).ToList();
        }

        private IEnumerable<ModDirectory> CreateDirectories(IEnumerable<Tuple<string[], ModFileConflictDescriptor>> directoryConflicts)
        {
            return directoryConflicts.GroupBy(k => k.Item1[_level]).OrderBy(g => g.Key)
                .Select(dc => new ModDirectory(dc.Key, _level + 1, dc.Select(k => k.Item2), _modFilter)).ToList();
        }
    }

    public class ModFileEntry : ModFileHolder
    {
        public ModFile File { get; }

        public ModFileConflictDescriptor ConflictDescriptor { get; }

        private bool _hasConflicts;

        public override bool HasConflicts => _hasConflicts;

        internal ModFileEntry(string name, ModFileConflictDescriptor conflictDescriptor, Func<Mod, bool> modFilter)
            : base(name)
        {
            ConflictDescriptor = conflictDescriptor;
            File = ConflictDescriptor.File;
            _hasConflicts = conflictDescriptor.ConflictingModFiles.Select(m => m.SourceMod).Count(modFilter) > 1;
        }

        public override void ApplyModFilter(Func<Mod, bool> filter)
        {
            this.RaiseAndSetIfChanged(ref _hasConflicts, ConflictDescriptor.ConflictingModFiles.Select(m => m.SourceMod).Count(filter) > 1, nameof(HasConflicts));
        }
    }
}