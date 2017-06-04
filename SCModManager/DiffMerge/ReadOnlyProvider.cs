using ICSharpCode.AvalonEdit.Editing;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;

namespace SCModManager.DiffMerge
{
    class ReadOnlyProvider : IReadOnlySectionProvider
    {
        public Comparison Comparison { get; set; }

        public Side Side { get; set; }

        public bool CanInsert(int offset)
        {
            if (Comparison.GetBlockContainingOffset(offset, Side)?.Block.IsEqual ?? true)
                return true;
            return false;
        }

        public IEnumerable<ISegment> GetDeletableSegments(ISegment segment)
        {
            var block = Comparison.GetBlockContainingOffset(segment.Offset, Side);
            do
            {
                int end = segment.EndOffset;
                if (end > block.EndOffset)
                    end = block.EndOffset;

                var start = segment.Offset;
                if (start < block.Offset)
                    start = block.Offset;

                if (block.Block.IsEqual)
                {
                    yield return new TextSegment { StartOffset = start, EndOffset = end, Length = end -start };
                } 
                // solve the drag problem

                block = block.GetNext();
            }
            while (block != null && block.EndOffset < segment.EndOffset) ;
        }
    }
}
