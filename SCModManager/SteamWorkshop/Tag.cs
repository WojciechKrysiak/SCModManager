using SCModManager.Ui;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Markup;

namespace SCModManager.SteamWorkshop
{
    public class TagDictionary : IDictionary
    {
        Dictionary<string, Tag> dictionary = new Dictionary<string, Tag>();

        public TagDictionary()
        { }


        public object this[object key]
        {
            get
            {
                return dictionary[key as string];
            }

            set
            {
                dictionary[key as string] = value as Tag;
            }
        }

        public int Count => dictionary.Count;

        [TypeConverter(typeof(ArrayTypeConverter))]
        public string[] DefaultChildTags { get; set; }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public ICollection Keys => dictionary.Keys;

        [TypeConverter(typeof(ArrayTypeConverter))]
        public string[] RootChildTags { get; set; }

        public object SyncRoot => this;

        public ICollection Values => dictionary.Values;

        public void Add(object key, object value)
        {
            var k = key as string;
            var v = value as Tag;

            if (v.SupportedChildTags == null)
            {
                if (v.ChildTags.Count > 0)
                {
                    v.SupportedChildTags = v.ChildTags.Keys.OfType<string>().ToArray();
                }
                else
                {
                    v.SupportedChildTags = DefaultChildTags;
                }
            }
            dictionary.Add(k, v);
        }

        public void Clear()
        {
            dictionary.Clear();
        }

        public bool Contains(object key)
        {
            return dictionary.ContainsKey(key as string);
        }

        public void CopyTo(Array array, int index)
        {
            (dictionary as ICollection).CopyTo(array, index);
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        public void Remove(object key)
        {
            dictionary.Remove(key as string);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }
    }

    [ContentProperty(nameof(Child))]
    public class Tag
    {
        public string NodeName { get; set; }

        public bool IsBlockLevel { get; set; }

        public bool SupportsLineBreaks { get; set; }

        public bool NoTextContent { get; set; }

        public string ContentTemplate { get; set; }

        [TypeConverter(typeof(ArrayTypeConverter))]
        public string[] SupportedChildTags { get; set; }

        public string Regex { get; set; }

        public List<Attribute> Attributes { get; set; } = new List<Attribute>();

        public TagDictionary ChildTags { get; set; } = new TagDictionary();

        public Tag Child { get; set; }
    }

    public class Attribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
