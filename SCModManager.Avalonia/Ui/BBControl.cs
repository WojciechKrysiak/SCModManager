using Avalonia;
using Avalonia.Controls.Html;
using Avalonia.Media.Imaging;
using CodeKicker.BBCode;
using System;
using System.IO;
using TheArtOfDev.HtmlRenderer.Core.Entities;

namespace SCModManager.Avalonia.Ui
{
	public class BBControl : HtmlLabel
    {
        public static StyledProperty<string> BBCodeProperty = AvaloniaProperty.Register<BBControl, string>("BBCode", string.Empty);

        public string BBCode
        {
            get { return (String)this.GetValue(BBCodeProperty); }
            set { this.SetValue(BBCodeProperty, value); }
        }

		static BBControl()
		{
			AffectsArrange(BBCodeProperty);
			BBCodeProperty.Changed.AddClassHandler<BBControl>(x => x.BBCodeChanged);
		}

		protected override void OnImageLoad(HtmlImageLoadEventArgs e)
		{
			if (e.Src.StartsWith("embed://"))
			{
				var source = e.Src.Replace("embed://", string.Empty);

				var resource = typeof(BBControl).Assembly.GetManifestResourceStream(source);

				if (resource != null)
				{
					var image = new Bitmap(resource);
					e.Callback(image);
					return;
				}
			}
			base.OnImageLoad(e);
		}

		protected override void OnStylesheetLoad(HtmlStylesheetLoadEventArgs e)
		{
			if (e.Src.StartsWith("embed://"))
			{
				var source = e.Src.Replace("embed://", string.Empty);

				var resource = typeof(BBControl).Assembly.GetManifestResourceStream(source);

				if (resource != null)
				{
					using (var reader = new StreamReader(resource))
						e.SetStyleSheet = reader.ReadToEnd();
					return;
				}
			}
			base.OnStylesheetLoad(e);
		}

		protected override void OnDataContextEndUpdate()
		{
			base.OnDataContextEndUpdate();
			ParseBBCode();
		}

		private BBCodeParser bbParser;



		private void BBCodeChanged(AvaloniaPropertyChangedEventArgs args)
		{
			ParseBBCode();
		}

		private void ParseBBCode()
		{
			if (bbParser == null)
				CreateBBParser();

			var value = BBCode;

			if (string.IsNullOrEmpty(value))
				Text = string.Empty;

			var innerHtml = this.bbParser.ToHtml(value);
			Text = string.Format(template, innerHtml);
		}

		private void CreateBBParser()
		{
			bbParser = new BBCodeParser(ErrorMode.ErrorFree, null, new[]
				{
					new BBTag("b", "<b>", "</b>"),
					new BBTag("h1", "<h1>", "</h1>"){ SuppressFirstNewlineAfter = true },
					new BBTag("i", "<span style=\"font-style:italic;\">", "</span>"),
					new BBTag("u", "<span style=\"text-decoration:underline;\">", "</span>"),
					new BBTag("code", "<pre class=\"prettyprint\">", "</pre>"){ StopProcessing = true, SuppressFirstNewlineAfter = true },
					new BBTag("img", "<img src=\"${content}\" />", "", false, true),
					new BBTag("quote", "<blockquote><span class=\"attribution\">${name}</span>", "</blockquote>"){ SuppressFirstNewlineAfter = true,  GreedyAttributeProcessing = true },
					new BBTag("list", "<ul>", "</ul>"){ SuppressFirstNewlineAfter = true },
					new BBTag("*", "<li>", "</li>", true, false),
					new BBTag("url", "<a href=\"${href}\">", "</a>", new BBAttribute("href", ""), new BBAttribute("href", "href")),
				});
		}

		private string template = @"<html><head><link href = 'embed://SCModManager.Avalonia.Resources.WorkshopStylesheet.css' rel='stylesheet' type='text/css'></head><body>
				<div class='outerWrapper'>
				<div class='innerWrapper'>
					<div class='description'>
						<div class='descriptionTitle'>
							Description
						</div>
						{0}
					</div>
				</div>
				</div>
			</body></html>";
	}
}
