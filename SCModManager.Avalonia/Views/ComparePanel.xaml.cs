using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SCModManager.Avalonia.Views
{
    public class ComparePanel : UserControl
    {
        public ComparePanel()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
