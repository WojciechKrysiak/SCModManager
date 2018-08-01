using FParsec;
using System;
using System.Collections.Generic;
using System.Text;
using static CWTools.Parser.Types;
using CWTools.CSharp;
using static CWTools.Process.CK2Process;
using System.Linq;
using System.IO;

namespace PDXModLib.Utility
{
    internal class CWToolsAdapter
    {

		private CWToolsAdapter(EventRoot eventRoot)
		{
			Root = eventRoot;
		}

		private CWToolsAdapter(string error)
		{
			ParseError = error;
		}

		public static CWToolsAdapter Parse(string file, string contents)
		{
			var result = CWTools.Parser.CKParser.parseEventString(contents, file);
			if (result.IsSuccess)
				return new CWToolsAdapter(CWTools.Process.CK2Process.processEventFile(result.GetResult()));

			return new CWToolsAdapter(result.GetError());
		}

		public static CWToolsAdapter Parse(string file)
		{
			var result = CWTools.Parser.CKParser.parseEventFile(file);
			if (result.IsSuccess)
				return new CWToolsAdapter(CWTools.Process.CK2Process.processEventFile(result.GetResult()));

			return new CWToolsAdapter(result.GetError());
		}

		public EventRoot Root { get; }
		public string ParseError { get; }
	}
}
