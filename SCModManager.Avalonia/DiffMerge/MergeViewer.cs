using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using System;
using System.Linq;
using System.Windows;
using Avalonia;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Styling;

namespace SCModManager.DiffMerge
{
    public class MergeViewer : TextEditor, IStyleable
    {
        public static StyledProperty<Vector> ScrollOffsetProperty = AvaloniaProperty.Register<MergeViewer, Vector>(nameof(ScrollOffset),new Vector(), false, BindingMode.TwoWay);

        public Vector ScrollOffset
        {
            get { return (Vector)GetValue(ScrollOffsetProperty); }
            set { SetValue(ScrollOffsetProperty, value); }
        }


        public static StyledProperty<Comparison> ContentsProperty = AvaloniaProperty.Register<MergeViewer, Comparison>(nameof(Contents), null);

        public Comparison Contents
        {
            get { return GetValue(ContentsProperty) as Comparison; }
            set { SetValue(ContentsProperty, value); }
        }

        public static StyledProperty<Side> SideProperty = AvaloniaProperty.Register<MergeViewer, Side>(nameof(Side), Side.Left);

        public Side Side
        {
            get { return (Side)GetValue(SideProperty); }
            set { SetValue(SideProperty, value); }
        }


        public static StyledProperty<bool> HideWhitespaceProperty = AvaloniaProperty.Register<MergeViewer, bool>(nameof(HideWhiteSpace), false);

        public bool HideWhiteSpace
        {
            get { return (bool)GetValue(HideWhitespaceProperty); }
            set { SetValue(HideWhitespaceProperty, value); }
        }

        ResultBlock selectedBlock;
        private Colorizer colorizer;
        private ReadOnlyProvider readonlyProvider;
        private bool suspendScroll;

        static MergeViewer()
        {
            ScrollOffsetProperty.Changed.AddClassHandler<MergeViewer>(x => x.ScrollOffsetPropertyChanged); 
            ContentsProperty.Changed.AddClassHandler<MergeViewer>(x => x.ContentsChanged); 
            SideProperty.Changed.AddClassHandler<MergeViewer>(x => x.SideChanged); 
            HideWhitespaceProperty.Changed.AddClassHandler<MergeViewer>(x => x.HideWhitespaceChanged);

            AffectsRender(HideWhitespaceProperty);
        }

        public MergeViewer()
        {
            TextArea.TextView.ScrollOffsetChanged += TextView_ScrollOffsetChanged;
        }


		Type IStyleable.StyleKey => typeof(TextEditor);


        private void ScrollOffsetPropertyChanged(AvaloniaPropertyChangedEventArgs args)
        {
            var v = (Vector)args.NewValue;

            var rect = new Rect(v.X, v.Y, TextArea.TextView.Bounds.Width, TextArea.TextView.Bounds.Height);
            suspendScroll = true;
            TextArea.TextView.MakeVisible(rect);
        }

        private void ContentsChanged(AvaloniaPropertyChangedEventArgs args)
        {
            var nv = (Comparison)args.NewValue;
            var ov = (Comparison)args.OldValue;
            bool needsRedraw = true;

            if (colorizer == null)
            {
                colorizer = new Colorizer();
                TextArea.TextView.LineTransformers.Add(colorizer);
            }
            colorizer.Comparison = nv;
            colorizer.Side = Side;
            colorizer.HideWhiteSpace = HideWhiteSpace;

            if (readonlyProvider == null && Side == Side.Result)
            {
                readonlyProvider = new ReadOnlyProvider();
                TextArea.ReadOnlySectionProvider = readonlyProvider;
                needsRedraw = false;
            }

            if (readonlyProvider != null)
                readonlyProvider.Comparison = nv;

            if (ov != null)
            {
                ov.RedrawRequested -= RedrawRequested;
            }

            if (nv != null)
            {
                nv.RedrawRequested += RedrawRequested;
            }

            if (needsRedraw)
                TextArea.TextView.Redraw();
        }

        private void SideChanged(AvaloniaPropertyChangedEventArgs args)
        {
            var v = (Side)args.NewValue;
            bool needsRedraw = true;

            if (colorizer == null)
            {
                colorizer = new Colorizer();
                TextArea.TextView.LineTransformers.Add(colorizer);
                needsRedraw = false;
            }
            colorizer.Side = v;
            colorizer.HideWhiteSpace = HideWhiteSpace;
            colorizer.Comparison = Contents;

            if (readonlyProvider == null && v == Side.Result)
            {
                readonlyProvider = new ReadOnlyProvider();
                TextArea.ReadOnlySectionProvider = readonlyProvider;
            }

            if (readonlyProvider != null)
                readonlyProvider.Side = v;
            if (needsRedraw)
                TextArea.TextView.Redraw();
        }


        private void HideWhitespaceChanged(AvaloniaPropertyChangedEventArgs args)
        {
            var v = (bool)args.NewValue;

            bool needsRedraw = true;
            if (colorizer == null)
            {
                colorizer = new Colorizer();
                TextArea.TextView.LineTransformers.Add(colorizer);
                needsRedraw = false;
            }
            colorizer.HideWhiteSpace = v;
            colorizer.Side = Side;
            colorizer.Comparison = Contents;
            if (needsRedraw)
                TextArea.TextView.Redraw();
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

#if false

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
#endif 
		
        protected override void OnPointerPressed(PointerPressedEventArgs e)
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

            base.OnPointerPressed(e);
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            var offfs = this.CaretOffset;
            base.OnTextInput(e);
        }
    }
}
