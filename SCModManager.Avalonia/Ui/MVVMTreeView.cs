using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Avalonia;
using Avalonia.Controls;

namespace SCModManager.Ui
{
    internal class MVVMTreeView : TreeView
    {
        public MVVMTreeView()
            : base()
        {
          //  this.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(___ICH);
        }

      // void ___ICH(object sender, RoutedPropertyChangedEventArgs<object> e)
      // {
      //     if (SelectedItem != null)
      //     {
      //         SetValue(SelectedProperty, SelectedValue);
      //     }
      // }

        public object Selected
        {
            get { return (object)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static StyledProperty<object> SelectedProperty = AvaloniaProperty.Register<MVVMTreeView, object>("Selected", null);
    }
}
