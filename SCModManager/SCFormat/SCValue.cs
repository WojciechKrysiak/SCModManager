using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager.SCFormat
{

    public class SCValue
    {
    }

    public class SCKeyValObject
    {
        public SCValue key { get; private set; }
        public SCValue value { get; private set; }

        public SCKeyValObject(SCValue key, SCValue value)
        {
            this.key = key;
            this.value = value;
        }

        public override string ToString()
        {
            if (key != null)
                return string.Format("{0} = {1}", key, value);
            return value.ToString();
        }
    }

    // Add Constructor.

    public class SCObject : SCValue, IEnumerable<SCKeyValObject>
    {
        public string Name { get; set; }

        public SCValue this[string text]
        {
            get
            {
                return contents.FirstOrDefault(kv => kv.key.ToString() == text)?.value; 
            }
        }

        List<SCKeyValObject> contents = new List<SCKeyValObject>();

        public void Add(SCValue key, SCValue value)
        {
            contents.Add(new SCKeyValObject(key, value));
        }

        public SCObject(SCIdentifier name = null)
        {
            Name = name?.Name;
        }

        public override string ToString()
        {
            return string.Format("{{\n{0}\n}}", string.Join(Environment.NewLine, contents));
        }

        public IEnumerator<SCKeyValObject> GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return contents.GetEnumerator();
        }
    }

    public class SCIdentifier : SCValue
    {
        public string Name { get; private set;}

        public SCIdentifier(string text) 
        {
            this.Name = text;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public class SCVariable : SCIdentifier
    {
        public SCVariable(string text) : base(text)
        {
        }

        public override string ToString()
        {
            return string.Format("@{0}", Name);
        }
    }

    public class SCString : SCValue
    {
        string text;

        public SCString(string text) 
        {
            this.text = text;
        }

        public override string ToString()
        {
            return string.Format("\"{0}\"", text);
        }
    }

    public class SCNumber : SCValue
    {
        double number;
        public SCNumber(string text) 
        {
            if (!double.TryParse(text, out number))
            {
                number = 0;
            }
        }

        public override string ToString()
        {
            return string.Format("{0}", number);
        }
    }

    public class SCPercent : SCValue
    {
        double value; 
        public SCPercent(string text) 
        {
            var stripped = text.Replace("%", string.Empty);
            if (double.TryParse(stripped, out value))
            {
                value /= 100;
            }
            else
                value = 0;
        }

        public override string ToString()
        {
            return string.Format("{0}%", value * 100);
        }
    }
}
