using System;
using System.Collections.Generic;
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
        private readonly IEnumerable<ModFileConflictDescriptor> _source;
        private static readonly char[] Separators = { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };

        private IEnumerable<ModFileHolder> _files;
        private bool _hasConflicts;

        public IEnumerable<ModFileHolder> Files => _files ?? (_files = CreateChildren());

        public override bool HasConflicts => _hasConflicts;

        public ModDirectory(string name, int level, IEnumerable<ModFileConflictDescriptor> source)
            : base(name)
        {
            _level = level;
            _source = source.ToList();
            Filename = name;
            _hasConflicts = _source.Any(mfcd => mfcd.ConflictingModFiles.Any());
        }

        private IEnumerable<ModFileHolder> CreateChildren()
        {
            var kids = _source.Select(m => Tuple.Create(m.File.Path.Split(Separators), m));
            List<ModFileHolder> result = new List<ModFileHolder>();
            foreach (var kid in kids.Where(t => t.Item1.Length > _level + 1).GroupBy(k => k.Item1[_level]).OrderBy(g => g.Key))
            {
                result.Add(new ModDirectory(kid.Key, _level + 1, kid.Select(k => k.Item2)));
            }

            foreach (var kid in kids.Where(t => t.Item1.Length == _level + 1))
            {
                result.Add(new ModFileEntry(kid.Item1.Last(), kid.Item2));
            }

            return result;
        }

        public static ModDirectory CreateRoot(ModConflictDescriptor conflictDescriptor)
        {
            return new ModDirectory(string.Empty, 0, conflictDescriptor.FileConflicts);
        }

        public override void ApplyModFilter(Func<Mod, bool> filter)
        {
            var conflcits = false;
            if (_files != null)
            {
                foreach (var modFileHolder in _files)
                {
                    modFileHolder.ApplyModFilter(filter);
                }
                conflcits = _files.Any(f => f.HasConflicts);
            }
            else 
                conflcits = _hasConflicts = _source.Any(mfcd => mfcd.ConflictingModFiles.Any(mf => filter(mf.SourceMod)));

            this.RaiseAndSetIfChanged(ref _hasConflicts, conflcits, nameof(HasConflicts));
        }
    }

    public class ModFileEntry : ModFileHolder
    {
        public ModFile File { get; }

        public ModFileConflictDescriptor ConflictDescriptor { get; }

        private bool _hasConflicts;

        public override bool HasConflicts => _hasConflicts;

        public ModFileEntry(string name, ModFileConflictDescriptor conflictDescriptor)
            : base(name)
        {
            ConflictDescriptor = conflictDescriptor;
            File = ConflictDescriptor.File;
            _hasConflicts = conflictDescriptor.ConflictingModFiles.Any();
        }

        public override void ApplyModFilter(Func<Mod, bool> filter)
        {
            this.RaiseAndSetIfChanged(ref _hasConflicts, ConflictDescriptor.ConflictingModFiles.Any(mf => filter(mf.SourceMod)), nameof(HasConflicts));
        }
    }
}