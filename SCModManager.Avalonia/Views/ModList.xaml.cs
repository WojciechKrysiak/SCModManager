using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SCModManager.Avalonia.Views
{
    public class ModList : UserControl
	{
        public ModList()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
