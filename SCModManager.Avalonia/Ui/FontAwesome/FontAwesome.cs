using Avalonia;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCModManager.Avalonia.Ui.FontAwesome
{
    public class FontAwesome : Shape
    {
        public static StyledProperty<AwesomeIcon> IconProperty = AvaloniaProperty.Register<FontAwesome, AwesomeIcon>("Icon", AwesomeIcon.None, false);

        public AwesomeIcon Icon
        {
            get { return (AwesomeIcon)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        static FontAwesome()
        {
            AffectsGeometry<FontAwesome>(IconProperty);
        }

        public FontAwesome()
        {
            Height = 12;
			Fill = Brushes.Black;
            Stretch = Stretch.Uniform;
        }

        protected override Geometry CreateDefiningGeometry()
        {
            if (Icon == AwesomeIcon.None)
                return new PathGeometry();

            return StreamGeometry.Parse(Icons.GetData(Icon));
        }
    }
}
