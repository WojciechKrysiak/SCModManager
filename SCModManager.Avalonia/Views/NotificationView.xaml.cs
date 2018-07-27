using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace SCModManager.Avalonia.Views
{
    public class NotificationView : Window
	{
        public NotificationView()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
