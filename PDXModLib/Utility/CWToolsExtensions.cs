using CWTools.Process;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDXModLib.Utility
{
    public static class CWToolsExtensions
    {
		public static Child Get(this Node node, string key)
		{
			return node.AllChildren.FirstOrDefault(c => c.IsNodeC  && c.node.Key == key || c.IsLeafC && c.leaf.Key == key);
		}

		public static string AsString(this Child child)
		{
			if (child.IsNodeC)
				return null;

			if (child.IsCommentC)
				return child.comment;

			if (child.IsLeafC)
				return child.leaf.Value.ToRawString();

			return child.lefavalue.Value.ToRawString();
		}
    }
}
