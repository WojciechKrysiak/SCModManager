using DiffMatchPatch;
using ICSharpCode.AvalonEdit.Document;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using ReactiveUI;

namespace SCModManager.DiffMerge
{
    public enum Side
    {
        Left,
        Right,
        Result 
    }

    public class Comparison
    {
        public ResultBlock Root { get; private set; }

        public Comparison(List<Diff> source)
        {
            ResultBlock current = null;
            ResultBlock result;

            int i = 0;
            for (; i < source.Count - 1; i++)
            {
                if (source[i].Operation.IsEqual)
                {
                    result = new ResultBlock(source[i].Text, current);
                    
                } else if (!source[i+1].Operation.IsEqual)
                {

                    result = new ResultBlock(source[i].Text, source[i + 1].Text, current);

                    i += 1;
                } else
                {
                    result = new ResultBlock(source[i].Text, source[i].Operation.IsInsert, current);
                }

                if (current == null)
                {
                    Root = result;
                }

                current = result;
                result.RebuildRequested += Result_RebuildRequested;
                result.RedrawRequested += Result_RedrawRequested;
            }

            if (i < source.Count)
            {
                if (source.Last().Operation.IsEqual)
                {
                    result = new ResultBlock(source[i].Text, current);
                }
                else
                {
                    result = new ResultBlock(source[i].Text, source[i].Operation.IsInsert, current);
                }

                if (current == null)
                {
                    Root = result;
                }

                result.RebuildRequested += Result_RebuildRequested;
                result.RedrawRequested += Result_RedrawRequested;
            }
        }

        public override string ToString()
        {
            return Root?.GetAsString(Side.Result);
        }

        private void Result_RedrawRequested(object sender, EventArgs e)
        {
            RedrawRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Result_RebuildRequested(object sender, RebuildRequestEventArgs e)
        {
            if (Root == sender)
            {
                Root = e.First;
            }

            (sender as ResultBlock).RebuildRequested -= Result_RebuildRequested;
            RebuildRequested?.Invoke(sender, e);
        }

        internal CalculatedBlock GetBlockContainingOffset(int offset, Side side)
        {
            var current = Root;

            var start = 0;

            while (current != null)
            {
                var len = current.Length(side);
                if (start <= offset && (start + len) > offset)
                {
                    return new CalculatedBlock
                    {
                        Side = side,
                        Offset = start,
                        EndOffset = start + len,
                        Block = current
                    };
                }
                start += len;
                current = current.NextBlock;
            }
            return null;
        }

        internal int GetOffsetToBlock(ResultBlock block, Side side)
        {
            var current = Root;
            var start = 0;

            while (current != null)
            {
                if (current == block)
                {
                    return start;
                }
                start += current.Length(side);
                current = current.NextBlock;
            }

            return -1;
        }

        internal void Append(string insertedText)
        {
            var block = Root;
            while(block?.NextBlock != null)
            {
                block = block.NextBlock;
            }

            var newBlock = new ResultBlock(insertedText, block);

        }

        public event EventHandler RedrawRequested;
        public event EventHandler<RebuildRequestEventArgs> RebuildRequested;
    }

    public class CalculatedBlock
    {
        public Side Side { get; set; }
        public int Offset { get; set; }
        public int EndOffset { get; set; }
        public ResultBlock Block { get; set; }

        public CalculatedBlock GetNext()
        {
            var nextBlock = Block.NextBlock;
            if (nextBlock == null)
            {
                return null;
            }

            return new CalculatedBlock
            {
                Side = Side,
                Offset = EndOffset,
                EndOffset = EndOffset + nextBlock.Length(Side),
                Block = nextBlock
            };
        }
    }

    public class ResultBlock
    {
        string left;
        string effectiveLeft;
        string right;
        string effectiveRight;
        string effectiveResult;

        bool _isSelected;

        bool hasLeft;
        bool hasRight;

        public bool IsConflict { get; set; }
        public bool IsEqual { get; set; }

        public ResultBlock PrevBlock { get; private set; }
        public ResultBlock NextBlock { get; set; }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    RedrawRequested?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public string this[Side side]
        {
            get
            {
                switch (side)
                {
                    case Side.Left: return effectiveLeft;
                    case Side.Right: return effectiveRight;
                }

                return effectiveResult;
            }
        }

        public bool HasSide(Side side)
        {
            switch (side)
            {
                case Side.Left: return hasLeft;
                case Side.Right: return hasRight;
            }
            return true;
        }

        public ICommand TakeLeft { get; }
        public ICommand TakeRight { get; }
        public ICommand TakeLeftThenRight { get; }
        public ICommand TakeRightThenLeft { get; }

        private ResultBlock(ResultBlock previous)
        {
            TakeLeft = ReactiveCommand.Create(() => ResolveAs(Side.Left));
            TakeRight = ReactiveCommand.Create(() => ResolveAs(Side.Right));
            TakeLeftThenRight = ReactiveCommand.Create(() => ResolveAs(Side.Left, Side.Right));
            TakeRightThenLeft = ReactiveCommand.Create(() => ResolveAs(Side.Right, Side.Left));
            PrevBlock = previous;
            if (PrevBlock != null)
            {
                PrevBlock.NextBlock = this;
            }
        }

        public ResultBlock(string both, ResultBlock previous)
            :this(previous)
        {
            left = right = effectiveLeft = effectiveRight = effectiveResult = both;
            IsEqual = hasLeft = hasRight = true;
        }


        public ResultBlock(string either, bool isAdd, ResultBlock previous)
            :this(previous)
        {
            if (isAdd)
            {
                left = effectiveLeft = effectiveResult = either;
                var rightLines = left.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                right = effectiveRight = string.Join(Environment.NewLine, rightLines.Select(l => new string(' ', l.Length)));
                hasLeft = true;
            }
            else
            {
                right = effectiveRight = effectiveResult = either;
                var leftLines = right.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                left = effectiveLeft = string.Join(Environment.NewLine, leftLines.Select(l => new string(' ', l.Length)));
                hasRight = true;
            }
        }

        public ResultBlock(string left, string right, ResultBlock previous)
            :this(previous)
        {
            var totalLen = Math.Max(left.Length, right.Length);

            var leftLines = left.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            var rightLines = right.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            effectiveLeft = left;
            effectiveRight = right;

            if (leftLines.Length < rightLines.Length)
            {
                for (int i = leftLines.Length; i < rightLines.Length; i++)
                {
                    effectiveLeft += string.Concat(new string(' ', rightLines[i].Length), Environment.NewLine);
                }
            } else if (leftLines.Length > rightLines.Length)
            {
                for (int i = rightLines.Length; i < leftLines.Length; i++)
                {
                    effectiveRight += string.Concat(new string(' ', leftLines[i].Length), Environment.NewLine);
                }
            } 

            effectiveResult = string.Empty;
            for (int i = 0; i < Math.Max(leftLines.Length, rightLines.Length); i++)
            {
                var l = i < leftLines.Length ? leftLines[i] : string.Empty;
                var r = i < rightLines.Length ? rightLines[i] : string.Empty;

                var ll = Math.Max(l.Length, r.Length);

                effectiveResult += string.Concat(new string(' ', ll), Environment.NewLine);
            }

            effectiveLeft = effectiveLeft.TrimEnd(Environment.NewLine.ToArray());
            effectiveRight = effectiveRight.TrimEnd(Environment.NewLine.ToArray());
            effectiveResult = effectiveResult.TrimEnd(Environment.NewLine.ToArray());

            this.left = left;
            this.right = right;

            hasLeft = hasRight = true;

            IsConflict = true;
        }

        public void ResolveAs(Side side)
        {
            if (!HasSide(side))
            {
                if (PrevBlock != null)
                {
                    PrevBlock.NextBlock = NextBlock;
                }
                if (NextBlock != null)
                {
                    NextBlock.PrevBlock = PrevBlock;
                }
                RebuildRequested?.Invoke(this, new RebuildRequestEventArgs(this));
            }
            else
            {
                ResultBlock result;

                if (side == Side.Left)
                {
                    result = new ResultBlock(left, PrevBlock);
                } else
                {
                    result = new ResultBlock(right, PrevBlock);
                }

                result.NextBlock = NextBlock;
                if (NextBlock != null)
                {
                    NextBlock.PrevBlock = result;
                }
                RebuildRequested?.Invoke(this, new RebuildRequestEventArgs(result));
            }
        }

        public void ResolveAs(Side first, Side second)
        {
            RebuildRequestEventArgs args;

            ResultBlock firstBlock = null;
            ResultBlock secondBlock = null;

            var current = this;
            if (HasSide(first))
            {
                ResultBlock result;

                if (first == Side.Left)
                {
                    result = new ResultBlock(left, PrevBlock);
                }
                else
                {
                    result = new ResultBlock(right, PrevBlock);
                }

                result.NextBlock = NextBlock;

                if (NextBlock != null)
                {
                    current.NextBlock.PrevBlock = result;
                }
                firstBlock = current = result;
            }

            if (HasSide(second))
            {
                ResultBlock result;

                var prev = HasSide(first) ? current : PrevBlock;

                if (second == Side.Left)
                {
                    result = new ResultBlock(left, prev);
                }
                else
                {
                    result = new ResultBlock(right, prev);
                }

                result.NextBlock = NextBlock;
                if (NextBlock != null)
                {
                    current.NextBlock.PrevBlock = result;
                }

                if (current == this)
                {
                    firstBlock = current = result;
                } else
                {
                    secondBlock = result;
                }
            }

            args = new RebuildRequestEventArgs(firstBlock, secondBlock);


            RebuildRequested?.Invoke(this, args);
        }

        public int Length(Side side)
        {
            return this[side].Length;
        }

        internal string GetAsString(Side side)
        {
            StringBuilder sb = new StringBuilder();

            AddToString(sb, side);

            return sb.ToString();
        }

        private void AddToString(StringBuilder sb, Side side)
        {
            sb.Append(this[side]);
            NextBlock?.AddToString(sb, side);
        }

        internal void Update(int delta, DocumentChangeEventArgs e)
        {
            // assumption is that the block is equal in both sides.

            string output = this.effectiveResult;

            if (e.InsertionLength > 0)
            {
                output = output.Insert(delta, e.InsertedText.Text);
            }

            if (e.RemovalLength > 0)
            {
                output = output.Remove(delta, e.RemovalLength);
            }

            effectiveResult = output;

            RedrawRequested?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler RedrawRequested;
        public event EventHandler<RebuildRequestEventArgs> RebuildRequested;

    }

    public class RebuildRequestEventArgs : EventArgs
    {
        public ResultBlock First { get; }
        public ResultBlock Second { get; }

        public RebuildRequestEventArgs(ResultBlock first, ResultBlock second)
        {
            First = first;
            Second = second;
        }

        public RebuildRequestEventArgs(ResultBlock first)
        {
            First = first;
        }
    }

}
