using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using NLog;
using SCModManager.Avalonia.DiffMerge;
using System;
using System.ComponentModel;
using System.Linq;

namespace SCModManager.Avalonia.Ui
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
		private Point lastPointerReleased;


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

		protected override void OnTemplateApplied(TemplateAppliedEventArgs e)
		{
			base.OnTemplateApplied(e);

			if (ContextMenu != null)
				ContextMenu.ContextMenuOpening += OnContextMenuOpening;
		}


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

		ILogger _logger = LogManager.GetCurrentClassLogger();

        private void OnContextMenuOpening(object sender, CancelEventArgs e)
        {
			e.Cancel = true;

            var pos = this.GetPositionFromPoint(lastPointerReleased);

			if (pos != null)
			{
				var offs = Document.GetOffset(pos.Value.Location);
				var block = Contents.GetBlockContainingOffset(offs, Side);
				if (block != null && !block.Block.IsEqual)
				{
					ContextMenu.Items = new[] {
					new MenuItem { Header = "Take left", Command = block.Block.TakeLeft },
					new MenuItem { Header = "Take right", Command = block.Block.TakeRight },
					new MenuItem { Header = "Take left then right", Command = block.Block.TakeLeftThenRight },
					new MenuItem { Header = "Take right then left", Command = block.Block.TakeRightThenLeft }
					};
					e.Cancel = false;
				}
				else
					_logger.Debug($"Block not found or is equal for point {lastPointerReleased}");
			}
			else
				_logger.Debug($"Pos not found for point {lastPointerReleased}");

        }
		
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            lastPointerReleased = e.GetPosition(this);

			_logger.Debug($"OnPointerPressed at {lastPointerReleased}, ContextMenu is null: {ContextMenu == null}");

			var pos = this.GetPositionFromPoint(lastPointerReleased);
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
