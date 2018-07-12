using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using PDXModLib.ModData;
using ReactiveUI;
using System.Text.RegularExpressions;

namespace SCModManager.DiffMerge
{
    public class MergeProcess : ReactiveObject
    {
        public static DiffMatchPatch.DiffMatchPatch DiffModule = new DiffMatchPatch.DiffMatchPatch(1f, (short)128, 0, 0.3f, 1000, 512, 0.2f, (short)64);

        readonly MergedModFile file;
        private ModFileToMerge _left;
        private ModFileToMerge _right;
        private ModFileToMerge _result;

        private Comparison _comparison;

        private ObservableCollection<ModFileToMerge> _sourceFiles  = new ObservableCollection<ModFileToMerge>();

        private readonly Subject<bool> _canSaveMerge = new Subject<bool>();

        private double[] _overviewMap;

        private TextDocument _rightDocument = new TextDocument();
        private TextDocument _leftDocument = new TextDocument();
        private Vector _scrollOffset;
        private TextDocument _resultDocument = new TextDocument();
        private bool _hideWhiteSpace;

        public bool HideWhiteSpace
        {
            get { return _hideWhiteSpace; }
            set
            {
                this.RaiseAndSetIfChanged(ref _hideWhiteSpace, value);
            }
        }

        public Vector ScrollOffset
        {
            get { return _scrollOffset; }
            set
            {
                this.RaiseAndSetIfChanged(ref _scrollOffset, value);
            }
        }

        public IReactiveCollection<ModFileToMerge> LeftSelection { get; }

        public ModFileToMerge Left {
            get { return _left; }
            set
            {
                this.RaiseAndSetIfChanged(ref _left, value);
                RightSelection.Reset();
                UpdateCompareContent();
            }
        }

        public TextDocument LeftDocument
        {
            get { return _leftDocument; }
            set { this.RaiseAndSetIfChanged(ref _leftDocument, value); }
        }

        public Comparison Comparison
        {
            get { return _comparison; }
            private set
            {
                this.RaiseAndSetIfChanged(ref _comparison, value);
            }
        }

        public IReactiveCollection<ModFileToMerge> RightSelection { get; }

        public ModFileToMerge Right
        {
            get { return _right; }
            set
            {
                this.RaiseAndSetIfChanged(ref _right, value);
                LeftSelection.Reset();
                UpdateCompareContent();
            }
        }

        public TextDocument RightDocument
        {
            get { return _rightDocument; }
            set { this.RaiseAndSetIfChanged(ref _rightDocument, value); }
        }

        public TextDocument ResultDocument
        {
            get { return _resultDocument; }
            set
            {
                if (_resultDocument != null)
                    _resultDocument.Changed -= _resultDocument_Changed;

                this.RaiseAndSetIfChanged(ref _resultDocument, value);

                if (_resultDocument != null)
                    _resultDocument.Changed += _resultDocument_Changed;
            }
        }

        public double[] OverviewMap
        {
            get { return _overviewMap; }
            set { this.RaiseAndSetIfChanged(ref _overviewMap, value); }
        }

        public MergeProcess(MergedModFile fileToMerge)
        {
            file = fileToMerge;
            _result = new ModFileToMerge(file);
            _sourceFiles = new ObservableCollection<ModFileToMerge>(file.SourceFiles.Select(f => new ModFileToMerge(f)));
            _sourceFiles.Add(_result);
            _sourceFiles.CollectionChanged += SourceFilesCollectionChanged;

            LeftSelection = _sourceFiles.CreateDerivedCollection(f => f, f => f?.RawContents != null && f != Right);
            RightSelection = _sourceFiles.CreateDerivedCollection(f => f, f => f?.RawContents != null && f != Left);

            Left = LeftSelection.First();
            Right = RightSelection.First();

            PickLeft = ReactiveCommand.Create(DoPickLeft);
            PickRight = ReactiveCommand.Create(DoPickRight);
            SaveMerge = ReactiveCommand.Create(DoSaveMerge, _canSaveMerge); 

            Reset = ReactiveCommand.Create(DoReset);
        }

        private void SourceFilesCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            switch (notifyCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                    foreach (var fileToMerge in notifyCollectionChangedEventArgs.OldItems.OfType<ModFileToMerge>())
                    {
                        file.RemoveSourceFile(fileToMerge.Source);
                    }
                    return;
            }
        }

        public ICommand Reset { get; }
        

        private void DoReset()
        {
            Left = null;
            Left = LeftSelection.First();
            Right = null;
            Right = RightSelection.First();
        }

        private void _resultDocument_Changed(object sender, DocumentChangeEventArgs e)
        {
            var block = Comparison.GetBlockContainingOffset(e.Offset, Side.Result);

            if (block != null)
            {
                var delta = e.Offset - block.Offset;

                block.Block.Update(delta, e);
            } else
            {
                Comparison.Append(e.InsertedText.Text);
            }
        }

        public void Remove(ModFile toRemove)
        {
            _sourceFiles.Remove(_sourceFiles.FirstOrDefault(f => f.Source == toRemove));
            if (file.Resolved)
            {
                Left = Right = null;
            }
            else
            {
                LeftSelection.Reset();
                Left = LeftSelection.First();
                Right = RightSelection.First();
            }
            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateCompareContent()
        {
            if (Left != null && Right != null)
            {
                var diff = DiffModule.DiffMain(Left.RawContents, Right.RawContents);

                DiffModule.DiffCleanupSemantic(diff);
                if (Comparison != null)
                {
                    Comparison.RebuildRequested -= Comparison_RebuildRequested;
                }

                Comparison = new Comparison(diff);

                Comparison.RebuildRequested += Comparison_RebuildRequested;

                LeftDocument.Text = Comparison.Root?.GetAsString(Side.Left);
                RightDocument.Text = Comparison.Root?.GetAsString(Side.Right);


                ResultDocument.Changed -= _resultDocument_Changed;
                ResultDocument.Text = Comparison.Root?.GetAsString(Side.Result);
                ResultDocument.Changed += _resultDocument_Changed;
            }
        }

        private void Comparison_RebuildRequested(object sender, RebuildRequestEventArgs e)
        {

            var prevBlock = sender as ResultBlock;
            int start = Comparison.GetOffsetToBlock(e.First, Side.Left);
            int length = prevBlock.Length(Side.Left);
            string text = e.First[Side.Left];

            if (e.Second != null)
            {
                text += e.Second[Side.Left];
            }

            LeftDocument.Replace(start, length, text);

            start = Comparison.GetOffsetToBlock(e.First, Side.Right);
            length = prevBlock.Length(Side.Right);
            text = e.First[Side.Right];

            if (e.Second != null)
            {
                text += e.Second[Side.Right];
            }

            RightDocument.Replace(start, length, text);

            start = Comparison.GetOffsetToBlock(e.First, Side.Result);
            length = prevBlock.Length(Side.Result);
            text = e.First[Side.Result];

            if (e.Second != null)
            {
                text += e.Second[Side.Result];
            }

            ResultDocument.Changed -= _resultDocument_Changed;
            ResultDocument.Replace(start, length, text);
            ResultDocument.Changed += _resultDocument_Changed;

            _canSaveMerge.OnNext(AreAllConflictsResolved());
        }

        public ICommand PickLeft { get; } 

        private void DoPickLeft()
        {
            if (Right != _result)
                _sourceFiles.Remove(Right);
            else
                file.SaveResult(null);

            if (file.Resolved)
            {
                Left = Right = null;
            }
            else
            {
                RightSelection.Reset();
                Right = RightSelection.First();
            }

            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        public ICommand PickRight { get; }

        private void DoPickRight()
        {
            if (Left != _result)
                _sourceFiles.Remove(Left);
            else
                file.SaveResult(null);

            if (file.Resolved)
            {
                Left = Right = null;
            }
            else
            {
                LeftSelection.Reset();
                Left = LeftSelection.First();
            }
            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        public ICommand SaveMerge { get; }

        private bool AreAllConflictsResolved()
        {
            ResultBlock block = Comparison.Root;

            while (block != null)
            {
                
                if (block.IsConflict && 
                    !(HideWhiteSpace && block.IsWhiteSpace))
                    return false;
                block = block.NextBlock;
            }

            return true;
        }

        private void DoSaveMerge()
        {
            file.SaveResult(ResultDocument.Text);

            var left = Left;
            var right = Right;

            if (left != _result)
                _sourceFiles.Remove(left);
            if (right != _result)
                _sourceFiles.Remove(right);

            if (file.Resolved)
            {
                Left = Right = null;
            }
            else
            {
                LeftSelection.Reset();
                Left = _result;
                Right = RightSelection.First();
            }
            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler FileResolved;

    }
}
