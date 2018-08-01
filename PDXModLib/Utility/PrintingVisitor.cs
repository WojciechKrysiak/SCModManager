using CWTools.ExtensionPoints;
using CWTools.Process;
using System;
using System.Collections.Generic;
using System.Text;

namespace PDXModLib.Utility
{
	public class PrintingVisitor : StatementVisitor
	{
		private const int SpacesPerIndent = 4;
		private readonly int indentLevel;
		private IndentingPersister persistent;

		public PrintingVisitor()
		{
			indentLevel = -1;
			persistent = new IndentingPersister();
		}

		private PrintingVisitor(int indentLevel, IndentingPersister persistentData)
		{
			this.indentLevel = indentLevel;
			this.persistent = persistentData;
		}

		public override void Visit(Node value)
		{
			if (this.indentLevel >= 0)
			{
				if (value.Key != null)
				{
					persistent.Append(indentLevel, value.Key);
					persistent.Append(" = {");
				}
				else
					persistent.Append(indentLevel, "{");

				if (value.All.Length == 0)
				{
					persistent.Append("}");
					return;
				}
				persistent.AppendLine();
			}


			var inner = new PrintingVisitor(indentLevel + 1, persistent);

			foreach (var child in value.All)
			{
				inner.Visit(child);
				persistent.AppendLine();
			}

			if (this.indentLevel >= 0)
				persistent.Append(indentLevel, "}");

		}

		public override void Visit(Leaf value)
		{
			persistent.Append(indentLevel, value.Key);
			persistent.Append(" = ");
			persistent.Append(value.Value.ToString());
		}

		public override void Visit(LeafValue value)
		{
			persistent.Append(indentLevel, value.Value.ToString());
		}

		public override void Visit(string comment)
		{
			persistent.Append(indentLevel, $"#{comment}");
			persistent.AppendLine();
		}

		public string Result => persistent.GetResult();

		private class IndentingPersister
		{
			private string[] indents;
			public StringBuilder Builder { get; }
			public IndentingPersister()
			{
				indents = new string[0];
				Builder = new StringBuilder();
			}

			private string GetIndent(int level)
			{
				if (indents.Length <= level)
				{
					Array.Resize(ref indents, level + 1);
				}

				if (indents[level] == null)
					indents[level] = new string(' ', SpacesPerIndent * level);

				return indents[level];
			}

			public void Append(string value)
			{
				Builder.Append(value);
			}

			public void Append(int indent, string value)
			{
				Builder.Append(GetIndent(indent));
				Builder.Append(value);
			}

			public void AppendLine()
			{
				Builder.AppendLine();
			}

			public string GetResult() => Builder.ToString();
		}
	}
}
