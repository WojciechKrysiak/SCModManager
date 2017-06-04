using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using ICSharpCode.AvalonEdit.Document;
using SCModManager.ModData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace SCModManager.DiffMerge
{
    public class MergeProcess : ObservableObject
    {
        public static DiffMatchPatch.DiffMatchPatch DiffModule = new DiffMatchPatch.DiffMatchPatch(1f, (short)128, 0, 0.3f, 1000, 512, 0.2f, (short)64);

        MergedModFile file;
        private ModFile _left;
        private ModFile _right;

        private ModFile _result;
        private Comparison _comparison;

        public Vector ScrollOffset
        {
            get { return _scrollOffset; }
            set
            {
                Set(ref _scrollOffset, value);
            }
        }


        public IEnumerable<ModFile> LeftSelection
        {
            get {
                return file.SourceFiles.Concat(new[] { _result }).Except(new[] { _right, null }).ToList();
            }
        }

        public ModFile Left {
            get { return _left; }
            set
            {
                Set(ref _left, value);
                RaisePropertyChanged(nameof(RightSelection));
                UpdateCompareContent();
            }
        }

        public TextDocument LeftDocument
        {
            get { return _leftDocument; }
            set { Set(ref _leftDocument, value); }
        }

        public Comparison Comparison
        {
            get { return _comparison; }
            private set
            {
                Set(ref _comparison, value);
            }
        }
        public IEnumerable<ModFile> RightSelection
        {
            get
            {
                return file.SourceFiles.Concat(new[] { _result }).Except(new[] { _left, null }).ToList();
            }
        }

        public ModFile Right
        {
            get { return _right; }
            set
            {
                Set(ref _right, value);
                RaisePropertyChanged(nameof(LeftSelection));
                UpdateCompareContent();
            }
        }

        public TextDocument RightDocument
        {
            get { return _rightDocument; }
            set { Set(ref _rightDocument, value); }
        }

        public TextDocument ResultDocument
        {
            get { return _resultDocument; }
            set { Set(ref _resultDocument, value); }
        }

        public MergeProcess(MergedModFile fileToMerge)
        {
            file = fileToMerge;
            Left = LeftSelection.First();
            Right = RightSelection.First();

            PickLeft = new RelayCommand(DoPickLeft);
            PickRight = new RelayCommand(DoPickRight);
            SaveMerge = new RelayCommand(DoSaveMerge, AreAllConflictsResolved);

            _resultDocument.Changed += _resultDocument_Changed;
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
            }
            else
            {
                Left = LeftSelection.First();
                Right = RightSelection.First();
            }
            RaisePropertyChanged(nameof(LeftSelection));
            RaisePropertyChanged(nameof(RightSelection));
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

            SaveMerge.RaiseCanExecuteChanged();
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
            RaisePropertyChanged(nameof(LeftSelection));
            RaisePropertyChanged(nameof(RightSelection));
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
            RaisePropertyChanged(nameof(LeftSelection));
            RaisePropertyChanged(nameof(RightSelection));
            FileResolved?.Invoke(this, EventArgs.Empty);
        }

        public RelayCommand SaveMerge { get; }

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
            RaisePropertyChanged(nameof(LeftSelection));
            RaisePropertyChanged(nameof(RightSelection));
            FileResolved?.Invoke(this, EventArgs.Empty);

        }

        public event EventHandler FileResolved;

    }
}
