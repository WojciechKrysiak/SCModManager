using ICSharpCode.AvalonEdit.Document;
using System.Windows;
using PDXModLib.ModData;
using ReactiveUI;

namespace SCModManager.DiffMerge
{
    public class ComparisonContext : ReactiveObject
    {
        private TextDocument _leftDocument;
        private Comparison _comparison;
        private TextDocument _rightDocument;
        private Vector _scrollOffset;

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

        public TextDocument RightDocument
        {
            get { return _rightDocument; }
            set { this.RaiseAndSetIfChanged(ref _rightDocument, value); }
        }


        public Vector ScrollOffset
        {
            get { return _scrollOffset; }
            set
            {
                this.RaiseAndSetIfChanged(ref _scrollOffset, value);
            }
        }

        public ComparisonContext(ModFile left, ModFile right)
        {
            LeftDocument = new TextDocument();
            RightDocument = new TextDocument();

            var diff = MergeProcess.DiffModule.DiffMain(left.RawContents, right.RawContents);

            MergeProcess.DiffModule.DiffCleanupSemantic(diff);

            Comparison = new Comparison(diff);

            RightDocument.Text = Comparison.Root?.GetAsString(Side.Right);

            LeftDocument.Text = Comparison.Root?.GetAsString(Side.Left);

        }
    }
}
