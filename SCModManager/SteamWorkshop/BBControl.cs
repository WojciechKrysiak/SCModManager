using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Media;

namespace SCModManager.SteamWorkshop
{
    public class BBControl : UserControl
    {
        public static readonly DependencyProperty BBCodeProperty = DependencyProperty.Register(
            "BBCode", typeof(String), typeof(BBControl), new PropertyMetadata(OnBBPropertyChanged));
        public string BBCode
        {
            get { return (String)this.GetValue(BBCodeProperty); }
            set { this.SetValue(BBCodeProperty, value); }
        }

        public static readonly DependencyProperty TagsProperty = DependencyProperty.Register(
          "Tags", typeof(TagDictionary), typeof(BBControl), new PropertyMetadata(OnBBPropertyChanged));
        public TagDictionary Tags
        {
            get { return this.GetValue(TagsProperty) as TagDictionary; }
            set { this.SetValue(TagsProperty, value); }
        }

        private static void OnBBPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is BBControl)
            {
                var parser = obj as BBControl;
                parser.Parse();
            }
        }

        private static string ContentPlaceholder = "$content";
        private static string ParameterPlaceholder = "$parameter";

        private static string TagPattern = @"(\[\s*(?<tag>{0})(?:\s*=\s*(?<param>[^\]]*))?\](?<ts>)(\s)*(?<content>.+?)(\s)*\[/\s*(?<-ts>\k<tag>)\s*\])(?(ts)(?!))";
        private static string TextPattern = @"(?<text>[^\r\n]+?(\z|(?=\[({0})(?:\s*=\s*(?<param>[^\]]*))?\]|\r\n|\n\r|\n|\r)))";
        private static string NewlinePattern = @"(?<newline>\r\n|\n\r|\n|\r)";

        private string BuildRegexPattern(Tag tag)
        {
            if (tag.Regex != null)
                return tag.Regex;

            List<string> result = new List<string>();
            result.Add(String.Format(TagPattern, string.Join("|", tag.SupportedChildTags)));

            if (!tag.NoTextContent)
                result.Add(String.Format(TextPattern, string.Join("|", tag.SupportedChildTags)));

            if (tag.SupportsLineBreaks)
                result.Add(NewlinePattern);


            return $"({string.Join("|", result)})";
        }

        XmlNode GetBlockLevel(XmlNode source)
        {
            switch (source.Name)
            {
                case "BlockUIContainer":
                case "List":
                case "Paragraph":
                case "Section":
                case "Table": return source.ParentNode;
            }

            return GetBlockLevel(source.ParentNode);
        }

        private void Parse(XmlNode topLevel, XmlNode parent, string bbCode, Tag parentTag)
        {
            var regexString = BuildRegexPattern(parentTag);

            var matches = Regex.Matches(bbCode, regexString, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

            var tags = parentTag?.ChildTags.Count > 0 ? parentTag?.ChildTags : Tags;

            foreach (var match in matches.OfType<Match>())
            {
                if (match.Groups["tag"].Success)
                {
                    var tag = match.Groups["tag"].Value.ToLower();

                    if (tags.Contains(tag))
                    {
                        List<Tag> nodeStack = new List<Tag>();
                        List<XmlElement> xamlStack = new List<XmlElement>();

                        nodeStack.Add(tags[tag] as Tag);
                        xamlStack.Add(parent.OwnerDocument.CreateElement(nodeStack.Last().NodeName));

                        while (nodeStack.Last().Child != null)
                        {
                            nodeStack.Add(nodeStack.Last().Child);
                            xamlStack.Add(parent.OwnerDocument.CreateElement(nodeStack.Last().NodeName));
                        }

                        for (int i = 1; i < xamlStack.Count; i++)
                        {
                            xamlStack[i - 1].AppendChild(xamlStack[i]);
                        }

                        parent.AppendChild(xamlStack.First());

                        if (nodeStack.First().IsBlockLevel)
                        {
                            GetBlockLevel(parent).AppendChild(xamlStack.First());
                        }
                        else
                        {
                            parent.AppendChild(xamlStack.First());
                        }

                        var node = xamlStack.Last();
                        var tagNode = nodeStack.First();

                        if (tagNode.ContentTemplate != null)
                        {
                            var content = tagNode.ContentTemplate.Replace(ContentPlaceholder, match.Groups["content"].Value).Replace(ParameterPlaceholder, match.Groups["param"].Value);
                            node.InnerText = content;
                        }
                        else
                        {
                            Parse(topLevel, node, match.Groups["content"].Value, tagNode);
                        }

                        for (int i = 0; i < xamlStack.Count; i++)
                        {
                            foreach (var attribute in nodeStack[i].Attributes)
                            {
                                xamlStack[i].SetAttribute(attribute.Name, attribute.Value.Replace(ContentPlaceholder, match.Groups["content"].Value).Replace(ParameterPlaceholder, match.Groups["param"].Value));
                            }
                        }
                    }
                    else
                    {
                        var node = parent.OwnerDocument.CreateTextNode($"Missing node {tag}!");
                        parent.AppendChild(node);
                    }
                }
                else if (match.Groups["text"].Success)
                {
                    var node = parent.OwnerDocument.CreateTextNode(match.Groups["text"].Value);
                    parent.AppendChild(node);
                }
                else if (match.Groups["newline"].Success)
                {
                    var blockLevel = GetBlockLevel(parent);
                    var para = parent.OwnerDocument.CreateElement("Paragraph");
                    para.SetAttribute("Style", "{DynamicResource BlockBase}");
                    blockLevel.AppendChild(para);
                    parent = para;
                }
            }

        }

        private void Parse()
        {
            if (String.IsNullOrEmpty(BBCode) || Tags == null)
            {
                this.Content = string.Empty;
                return;
            }

            XmlDocument doc = new XmlDocument();

            var root = doc.CreateElement("FlowDocumentScrollViewer");
            root.SetAttribute("VerticalScrollBarVisibility", "Disabled");
            root.SetAttribute("MinZoom", "100");
            root.SetAttribute("MaxZoom", "100");
            doc.AppendChild(root);

            var first = doc.CreateElement("FlowDocument");
            first.SetAttribute("xml:space", "preserve");
            root.AppendChild(first);

            var para = doc.CreateElement("Paragraph");
            para.SetAttribute("Style", "{DynamicResource BlockBase}");
            first.AppendChild(para);

            var rootTag = new Tag { SupportedChildTags = Tags.RootChildTags, SupportsLineBreaks = true, IsBlockLevel = true };

            Parse(first, para, BBCode, rootTag);

            string xaml = doc.OuterXml;
            try
            {
                ParserContext pc = new ParserContext();
                pc.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                pc.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                this.Content = XamlReader.Load(new MemoryStream(Encoding.UTF8.GetBytes(xaml)), pc);

                var flowDocViewer = this.Content as FlowDocumentScrollViewer;

                flowDocViewer.AddHandler(FrameworkElement.MouseWheelEvent, new RoutedEventHandler(OnFlowDocumentMouseWheel), true);
            }
            catch (Exception ex)
            {
                this.Content = "Can't parse the Document!!" + Environment.NewLine + BBCode;
                this.ToolTip = ex.Message;
            }
        }

        public void OnFlowDocumentMouseWheel(object sender, RoutedEventArgs a)
        {
            a.Handled = false;
            return;
        }

    }
}
