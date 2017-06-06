using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SCModManager.Ui
{
    internal class MVVMTreeView : TreeView
    {
        public MVVMTreeView()
            : base()
        {
            this.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(___ICH);
        }

        void ___ICH(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedItem != null)
            {
                SetValue(SelectedProperty, SelectedItem);
            }
        }

        public object Selected
        {
            get { return (object)GetValue(SelectedProperty); }
            set { SetValue(SelectedProperty, value); }
        }

        public static readonly DependencyProperty SelectedProperty = DependencyProperty.Register("Selected", typeof(object), typeof(MVVMTreeView), new UIPropertyMetadata(null));
    }
}
