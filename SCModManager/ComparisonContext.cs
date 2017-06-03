using GalaSoft.MvvmLight;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SCModManager
{
    class ComparisonContext : ObservableObject
    {
        private TextDocument _leftDocument;
        private Comparison _comparison;
        private TextDocument _rightDocument;
        private Vector _scrollOffset;

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

        public TextDocument RightDocument
        {
            get { return _rightDocument; }
            set { Set(ref _rightDocument, value); }
        }


        public Vector ScrollOffset
        {
            get { return _scrollOffset; }
            set
            {
                Set(ref _scrollOffset, value);
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
