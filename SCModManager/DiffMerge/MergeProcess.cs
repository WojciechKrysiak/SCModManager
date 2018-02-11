using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Input;
using PDXModLib.ModData;
using ReactiveUI;

namespace SCModManager.DiffMerge
{
    public class MergeProcess : ReactiveObject
    {
        public static DiffMatchPatch.DiffMatchPatch DiffModule = new DiffMatchPatch.DiffMatchPatch(1f, (short)128, 0, 0.3f, 1000, 512, 0.2f, (short)64);

        readonly MergedModFile file;
        private ModFile _left;
        private ModFile _right;

        private ModFile _result;
        private Comparison _comparison;

        private readonly Subject<bool> _canSaveMerge = new Subject<bool>();

        public Vector ScrollOffset
        {
            get { return _scrollOffset; }
            set
            {
                this.RaiseAndSetIfChanged(ref _scrollOffset, value);
            }
        }

        public IEnumerable<ModFile> LeftSelection
        {
            get {
                return file.SourceFiles.Concat(new[] { _result }).Where(f => f != null && f != _right).ToList();
            }
        }

        public ModFile Left {
            get { return _left; }
            set
            {
                this.RaiseAndSetIfChanged(ref _left, value);
                this.RaisePropertyChanged(nameof(RightSelection));
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
        public IEnumerable<ModFile> RightSelection
        {
            get
            {
                return file.SourceFiles.Concat(new[] { _result }).Where(f => f != null && f != _left).ToList();
            }
        }

        public ModFile Right
        {
            get { return _right; }
            set
            {
                this.RaiseAndSetIfChanged(ref _right, value);
                this.RaisePropertyChanged(nameof(LeftSelection));
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
            set { this.RaiseAndSetIfChanged(ref _resultDocument, value); }
        }

        public MergeProcess(MergedModFile fileToMerge)
        {
            file = fileToMerge;
            Left = LeftSelection.First();
            Right = RightSelection.First();

            PickLeft = ReactiveCommand.Create(DoPickLeft);
            PickRight = ReactiveCommand.Create(DoPickRight);
            SaveMerge = ReactiveCommand.Create(DoSaveMerge, _canSaveMerge); 

            Reset = ReactiveCommand.Create(DoReset);

            _resultDocument.Changed += _resultDocument_Changed;
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
            file.SourceFiles.Remove(toRemove);
            if (file.SourceFiles.Count == 1)
            {
                Left = Right = null;
                file.SaveResult(file.SourceFiles.First().RawContents);
                file.SourceFiles.Clear();
            }
            else
            {
                Left = LeftSelection.First();
                Right = RightSelection.First();
            }
            this.RaisePropertyChanged(nameof(LeftSelection));
            this.RaisePropertyChanged(nameof(RightSelection));
            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        private TextDocument _rightDocument = new TextDocument();
        private TextDocument _leftDocument = new TextDocument();
        private Vector _scrollOffset;
        private TextDocument _resultDocument = new TextDocument();

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

                RightDocument.Text = Comparison.Root?.GetAsString(Side.Right);

                LeftDocument.Text = Comparison.Root?.GetAsString(Side.Left);

                ResultDocument.Text = Comparison.Root?.GetAsString(Side.Result);
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
            file.SaveResult(Left.RawContents);
            _result = file;

            file.SourceFiles.Remove(Left);
            file.SourceFiles.Remove(Right);

            if (file.SourceFiles.Count < 2)
            {
                Left = Right = null;
            }
            else
            {
                Left = _result;
                Right = RightSelection.First();
            }
            this.RaisePropertyChanged(nameof(LeftSelection));
            this.RaisePropertyChanged(nameof(RightSelection));
            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        public ICommand PickRight { get; }

        private void DoPickRight()
        {
            file.SaveResult(Right.RawContents);
            _result = file;

            file.SourceFiles.Remove(Right);
            file.SourceFiles.Remove(Left);
            if (file.SourceFiles.Count < 2)
            {
                Left = Right = null;
            }
            else
            {
                Left = _result;
                Right = RightSelection.First();
            }
            this.RaisePropertyChanged(nameof(LeftSelection));
            this.RaisePropertyChanged(nameof(RightSelection));
            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        public ICommand SaveMerge { get; }

        private bool AreAllConflictsResolved()
        {
            ResultBlock block = Comparison.Root;

            while (block != null)
            {
                if (block.IsConflict)
                    return false;
                block = block.NextBlock;
            }

            return true;
        }

        private void DoSaveMerge()
        {
            file.SaveResult(ResultDocument.Text);
            _result = file;

            file.SourceFiles.Remove(Right);
            file.SourceFiles.Remove(Left);
            if (file.SourceFiles.Count < 2)
            {
                Left = Right = null;
            }
            else
            {
                Left = _result;
                Right = RightSelection.First();
            }
            this.RaisePropertyChanged(nameof(LeftSelection));
            this.RaisePropertyChanged(nameof(RightSelection));
            FileResolved?.Invoke(this, EventArgs.Empty);

        }

        public event EventHandler FileResolved;

    }
}
