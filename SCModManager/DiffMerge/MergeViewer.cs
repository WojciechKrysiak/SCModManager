using ICSharpCode.AvalonEdit;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SCModManager.DiffMerge
{
    public class MergeViewer : TextEditor
    {
        public static DependencyProperty ScrollOffsetProperty = DependencyProperty.Register("ScrollOffset", typeof(Vector), typeof(MergeViewer), new FrameworkPropertyMetadata(new Vector(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ScrollOffsetPropertyChanged));

        public Vector ScrollOffset
        {
            get { return (Vector)GetValue(ScrollOffsetProperty); }
            set { SetValue(ScrollOffsetProperty, value); }
        }

        private static void ScrollOffsetPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var v = (Vector)e.NewValue;
            var mv = d as MergeViewer;

            var rect = new Rect(v.X, v.Y, mv.TextArea.TextView.ActualWidth, mv.TextArea.TextView.ActualHeight);
            mv.suspendScroll = true;
            mv.TextArea.TextView.MakeVisible(rect);
        }

        public static DependencyProperty ContentsProperty = DependencyProperty.Register("Contents", typeof(Comparison), typeof(MergeViewer), new FrameworkPropertyMetadata(null, ContentsChanged));

        public Comparison Contents
        {
            get { return GetValue(ContentsProperty) as Comparison; }
            set { SetValue(ContentsProperty, value); }
        }

        private static void ContentsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var nv = (Comparison)e.NewValue;
            var ov = (Comparison)e.OldValue;
            var mv = d as MergeViewer;

            if (mv.colorizer == null)
            {
                mv.colorizer = new Colorizer();
                mv.TextArea.TextView.LineTransformers.Add(mv.colorizer);
            }
            mv.colorizer.Comparison = nv;

            if (mv.readonlyProvider == null && mv.Side == Side.Result)
            {
                mv.readonlyProvider = new ReadOnlyProvider();
                mv.TextArea.ReadOnlySectionProvider = mv.readonlyProvider;
            }

            if (mv.readonlyProvider != null)
                mv.readonlyProvider.Comparison = nv;

            if (ov != null)
            {
                ov.RedrawRequested -= mv.RedrawRequested;
            }

            if (nv != null)
            {
                nv.RedrawRequested += mv.RedrawRequested;
            }

        }

        public static DependencyProperty SideProperty = DependencyProperty.Register("Side", typeof(Side), typeof(MergeViewer), new PropertyMetadata(Side.Left, SideChanged));

        public Side Side
        {
            get { return (Side)GetValue(SideProperty); }
            set { SetValue(SideProperty, value); }
        }


        private static void SideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var v = (Side)e.NewValue;
            var mv = d as MergeViewer;

            if (mv.colorizer == null)
            {
                mv.colorizer = new Colorizer();
                mv.TextArea.TextView.LineTransformers.Add(mv.colorizer);
            }
            mv.colorizer.Side = v;

            if (mv.readonlyProvider == null && v == Side.Result)
            {
                mv.readonlyProvider = new ReadOnlyProvider();
                mv.TextArea.ReadOnlySectionProvider = mv.readonlyProvider;
            }

            if (mv.readonlyProvider != null)
                mv.readonlyProvider.Side = v;
        }

        ResultBlock selectedBlock;
        private Colorizer colorizer;
        private ReadOnlyProvider readonlyProvider;
        private bool suspendScroll;

        public MergeViewer()
        {
            TextArea.TextView.ScrollOffsetChanged += TextView_ScrollOffsetChanged; ;
        }

        private void TextView_ScrollOffsetChanged(object sender, EventArgs e)
        {
            if (!suspendScroll)
            {
                ScrollOffset = TextArea.TextView.ScrollOffset;
            }
            suspendScroll = false;
        }

        private void RedrawRequested(object sender, EventArgs e)
        {
            TextArea.TextView.Redraw();
        }

        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            var delta = 0.0;

            foreach(var margin in TextArea.LeftMargins.OfType<FrameworkElement>())
            {
                delta += margin.ActualWidth;
            }

            var pos = this.GetPositionFromPoint(new Point(e.CursorLeft + delta, e.CursorTop));
            
            if (pos != null)
            {
                var offs = Document.GetOffset(pos.Value.Location);
                var block = Contents.GetBlockContainingOffset(offs, Side);
                if (block != null && !block.Block.IsEqual) 
                {
                    ContextMenu = new ContextMenu();

                    ContextMenu.Items.Add(new MenuItem { Header = "Take left", Command = block.Block.TakeLeft });
                    ContextMenu.Items.Add(new MenuItem { Header = "Take right", Command = block.Block.TakeRight });
                    ContextMenu.Items.Add(new MenuItem { Header = "Take left then right", Command = block.Block.TakeLeftThenRight });
                    ContextMenu.Items.Add(new MenuItem { Header = "Take right then left", Command = block.Block.TakeRightThenLeft });
                }
            }
            base.OnContextMenuOpening(e);
        }

        protected override void OnContextMenuClosing(ContextMenuEventArgs e)
        {
            ContextMenu = null;
            base.OnContextMenuOpening(e);
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            var vpos = e.GetPosition(this);
            var pos = this.GetPositionFromPoint(vpos);
            if (pos != null)
            {
                if (selectedBlock != null)
                {
                    selectedBlock.IsSelected = false;
                }

                var offs = Document.GetOffset(pos.Value.Location);
                var block = Contents.GetBlockContainingOffset(offs, Side);

                if (block != null && !block.Block.IsEqual)
                {
                    selectedBlock = block.Block;
                    selectedBlock.IsSelected = true;
                }
            }

            base.OnMouseUp(e);
        }

        protected override void OnTextInput(TextCompositionEventArgs e)
        {
            var offfs = this.CaretOffset;
            base.OnTextInput(e);
        }
    }
}
