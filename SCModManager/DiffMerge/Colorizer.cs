﻿using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Windows.Media;

namespace SCModManager.DiffMerge
{
    class Colorizer : DocumentColorizingTransformer
    {
        public Comparison Comparison { get; set; }

        public Side Side { get; set; }

        public bool HideWhiteSpace { get; set; }

        protected override void ColorizeLine(DocumentLine line)
        {
            var current = Comparison.GetBlockContainingOffset(line.Offset, Side);

            if (current == null)
            {
                return;
            }

            do
            {
                int start = Math.Max(line.Offset, current.Offset);
                int end = Math.Min(line.EndOffset, current.EndOffset);
                if (current.Block.IsSelected)
                {
                    this.ChangeLinePart(start, end, (vle) => {
                        vle.BackgroundBrush = Brushes.DarkGray;
                        vle.TextRunProperties.SetForegroundBrush(Brushes.White);
                    });
                }
                else if(!(current.Block.IsEqual || 
                         HideWhiteSpace && current.Block.IsWhiteSpace))
                {
                    if (current.Block.IsConflict)
                    {
                        this.ChangeLinePart(start, end, (vle) => vle.BackgroundBrush = Brushes.PaleVioletRed);
                    }
                    else if (current.Block.HasSide(Side))
                    {
                        this.ChangeLinePart(start, end, (vle) => vle.BackgroundBrush = Brushes.LightGreen);
                    }
                    else
                    {
                        this.ChangeLinePart(start, end, (vle) => vle.BackgroundBrush = Brushes.Gray);
                    }
                }

                current = current.GetNext();
            } while (current != null && line.EndOffset > current.Offset);
        }
    }
}
