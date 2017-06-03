using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace SCModManager
{
    [ContentProperty("Content")]

    class TypeBasedDataTemplateSelector : DataTemplateSelector
    {
        [Bindable(true)]
        public Collection<DataTemplate> Content { get; set; } = new Collection<DataTemplate>();

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var itemType = item?.GetType() ?? typeof(object);

            foreach(var template in Content)
            {
                if ((template.DataType as Type)?.IsAssignableFrom(itemType) ?? false)
                {
                    return template;
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
