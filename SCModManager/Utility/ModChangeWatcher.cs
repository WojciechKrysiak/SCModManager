using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using PDXModLib.Interfaces;

namespace SCModManager.Utility
{
    public sealed class ModChangeWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;

        private readonly Dictionary<string, FileSystemWatcher> _watchedPaths = new Dictionary<string, FileSystemWatcher>();

        private readonly Dictionary<string, string> _descriptorToContent = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _contentToDescriptor = new Dictionary<string, string>();

        private readonly Subject<string> _modAdded = new Subject<string>();
        private readonly Subject<string> _modDeleted = new Subject<string>();
        private readonly Subject<string> _modContentChanged= new Subject<string>();
        private readonly Subject<string> _modContentDeleted = new Subject<string>();

        public ModChangeWatcher(IGameConfiguration configuration)
        {
            _watcher = new FileSystemWatcher(configuration.ModsDir);

            ModAdded = _modAdded;//.ObserveOn(Application.Current.Dispatcher);
            ModDeleted = _modDeleted;//.ObserveOn(Application.Current.Dispatcher);
            ModContentChanged = _modContentChanged.Distinct().Delay(TimeSpan.FromMilliseconds(300));//.ObserveOn(Application.Current.Dispatcher);
            ModContentDeleted = _modContentDeleted;//.ObserveOn(Application.Current.Dispatcher);
        }

        public void Initialize()
        {
            _watcher.NotifyFilter = NotifyFilters.FileName;
            _watcher.Filter = "*.mod";
            _watcher.Created += DescriptorCreated;
            _watcher.Changed += DescriptorChanged;
            _watcher.Deleted += DescriptorDeleted;
        }

        private void DescriptorDeleted(object sender, FileSystemEventArgs e)
        {
            if (!_descriptorToContent.ContainsKey(e.FullPath))
                return;

            var content = _descriptorToContent[e.FullPath];
            _descriptorToContent.Remove(e.FullPath);
            _contentToDescriptor.Remove(e.FullPath);

            _modDeleted.OnNext(e.FullPath);
        }

        private void DescriptorChanged(object sender, FileSystemEventArgs e)
        {
            if (!_descriptorToContent.ContainsKey(e.FullPath))
                return; 

            _modContentChanged.OnNext(e.FullPath);
        }

        private void DescriptorCreated(object sender, FileSystemEventArgs e)
        {
            _modAdded.OnNext(e.FullPath);
        }


        public void MonitorMod(string descriptorPath, string contentPath)
        {
            _contentToDescriptor.Add(contentPath, descriptorPath);
            _descriptorToContent.Add(descriptorPath, contentPath);

            var watchedPath = contentPath;
            if (Path.HasExtension(watchedPath))
                watchedPath = Path.GetDirectoryName(watchedPath);

            if (_watchedPaths.ContainsKey(watchedPath) ||
                _watchedPaths.Keys.Any(k => watchedPath.Contains(k))) 
                return;

            // check if we already are watching a subdirectory
            var existing = _watchedPaths.Keys.FirstOrDefault(k => k.Contains(watchedPath));
            if (existing != null)
            {
                _watchedPaths[existing].Dispose();
                _watchedPaths.Remove(existing);
            }

            var pathWatcher = new FileSystemWatcher(watchedPath);
            pathWatcher.NotifyFilter = NotifyFilters.LastWrite;
            pathWatcher.Created += OnContentCreated;
            pathWatcher.Deleted += OnContentDeleted;
            pathWatcher.Changed += OnContentChanged;

            _watchedPaths.Add(watchedPath, pathWatcher);

            pathWatcher.EnableRaisingEvents = true;
        }

        private void OnContentChanged(object sender, FileSystemEventArgs e)
        {
            var contentPath = _contentToDescriptor.Keys.FirstOrDefault(k => e.FullPath.Contains(k));
            if (contentPath != null)
            {
                var descriptor = _contentToDescriptor[contentPath];
                _modContentChanged.OnNext(descriptor);
            }
        }

        private void OnContentDeleted(object sender, FileSystemEventArgs e)
        {
            if (_contentToDescriptor.ContainsKey(e.FullPath))
            {
                // make sure that mod updates do not delete/create the zips
                var descriptor = _contentToDescriptor[e.FullPath];
                _modContentDeleted.OnNext(descriptor);
            }

            var contentPath = _contentToDescriptor.Keys.FirstOrDefault(k => e.FullPath.Contains(k));
            if (contentPath != null)
            {
                var descriptor = _contentToDescriptor[contentPath];
                _modContentChanged.OnNext(descriptor);
            }
        }

        private void OnContentCreated(object sender, FileSystemEventArgs e)
        {
            if (_contentToDescriptor.ContainsKey(e.FullPath))
            {
                // make sure that mod updates do not delete/create the zips
                var descriptor = _contentToDescriptor[e.FullPath];
                _modContentChanged.OnNext(descriptor);
            }

            var contentPath = _contentToDescriptor.Keys.FirstOrDefault(k => e.FullPath.Contains(k));
            if (contentPath != null)
            {
                var descriptor = _contentToDescriptor[contentPath];
                _modContentChanged.OnNext(descriptor);
            }
        }

        public void Dispose()
        {
            _watcher.Dispose();
            foreach (var watchers in _watchedPaths.Values)
            {
                watchers.Dispose();
            }
        }


        /// <summary>
        /// Indicates that a mod descriptor has been added to the mods directory.
        /// Does not register the mod content for tracking.
        /// </summary>
        public IObservable<string> ModAdded { get; }

        /// <summary>
        /// Indicates that a mod descriptor has been deleted
        /// Stops tracking the content before firing this event.
        /// </summary>
        public IObservable<string> ModDeleted { get; }

        /// <summary>
        /// Indicates that mod content has been changed
        /// </summary>
        public IObservable<string> ModContentChanged { get; }

        /// <summary>
        /// This event indicates that mod content has been deleted.
        /// Indicates an error if the descriptor has not yet been deleted.
        /// </summary>
        public IObservable<string> ModContentDeleted { get; }

    }
}
