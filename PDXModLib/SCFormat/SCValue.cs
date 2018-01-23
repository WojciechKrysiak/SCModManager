using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDXModLib.SCFormat
{

    public abstract class SCValue
    {
        internal abstract void Serialize(StringBuilder writer);
    }

    public class SCKeyValObject
    {
        public SCValue Key { get; }
        public SCValue Cmp { get; }
        public SCValue Value { get; }

        public SCKeyValObject(SCValue key, SCValue cmp, SCValue value)
        {
            Key = key;
            Cmp = cmp;
            Value = value;
        }

        public SCKeyValObject(SCValue key, SCValue value)
            :this(key, new SCToken(Tokens.Eq),value)
        {
        }

        public static SCKeyValObject Create(string value)
        {
            return new SCKeyValObject(null, null, new SCString(value));
        }

        public static SCKeyValObject Create(string key, string value)
        {
            var k = new SCIdentifier(key);
            var t = new SCToken(Tokens.Eq);
            var v = new SCString(value);
            return new SCKeyValObject(k, t, v);
        }

        public static SCKeyValObject Create(string key, SCValue value)
        {
            var k = new SCIdentifier(key);
            var t = new SCToken(Tokens.Eq);
            return new SCKeyValObject(k, t, value);
        }

        public override string ToString()
        {
            return Key != null ? $"{Key}{Cmp}{Value}" : Value.ToString();
        }
    }

    public class SCObject : SCValue, IEnumerable<SCKeyValObject>
    {
        public string Name { get; set; }

        public SCValue this[string text]
        {
            get
            {
                return contents.FirstOrDefault(kv => kv.Key.ToString() == text)?.Value;
            }

            set
            {
                var kvo = contents.FirstOrDefault(kv => kv.Key.ToString() == text);

                if (kvo == null)
                {
                    contents.Add(new SCKeyValObject(new SCIdentifier(text), new SCToken(Tokens.Eq), value));
                }
                else
                {
                    var idx = contents.IndexOf(kvo);
                    contents.Remove(kvo);
                    contents.Insert(idx, new SCKeyValObject(new SCIdentifier(text), new SCToken(Tokens.Eq), value));
                }
            }
        }


        public SCValue this[SCValue val]
        {
            get
            {
                return contents.FirstOrDefault(kv => kv.Key.ToString() == val.ToString())?.Value;
            }

            set
            {
                var kvo = contents.FirstOrDefault(kv => kv.Key.ToString() == val.ToString());

                if (kvo == null)
                {
                    contents.Add(new SCKeyValObject(val, new SCToken(Tokens.Eq), value));
                }
                else
                {
                    var idx = contents.IndexOf(kvo);
                    contents.Remove(kvo);
                    contents.Insert(idx, new SCKeyValObject(val, new SCToken(Tokens.Eq), value));
                }
            }
        }

        private readonly List<SCKeyValObject> contents = new List<SCKeyValObject>();


        public void Add(SCKeyValObject obj)
        {
            contents.Add(obj);
        }


        public void Add(SCValue key, SCValue cmp, SCValue value)
        {
            contents.Add(new SCKeyValObject(key, cmp, value));
        }

        public void Remove(SCKeyValObject kvp)
        {
            contents.Remove(kvp);
        }

        public void Remove(string key)
        {
            var kvo = contents.FirstOrDefault(kv => kv.Key.ToString() == key);

            contents.Remove(kvo);
        }

        public SCObject(SCIdentifier name = null)
        {
            Name = name?.Name;
        }

        public override string ToString()
        {
            return $"{{\n{string.Join(Environment.NewLine, contents)}\n}}";
        }

        public IEnumerator<SCKeyValObject> GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return contents.GetEnumerator();
        }

        internal override void Serialize(StringBuilder writer)
        {
            writer.Append(Name);
            writer.AppendLine("{");

            var inner = new StringBuilder();

            WriteContents(inner);
            writer.Append("\t");
            writer.Append(inner.ToString().Replace(Environment.NewLine, $"{Environment.NewLine}\t"));

            writer.AppendLine("}");
        }

        private void WriteContents(StringBuilder writer)
        {
            foreach (var kvp in contents)
            {
                if (kvp.Key == null)
                {
                    kvp.Value.Serialize(writer);
                }
                else
                {
                    kvp.Key.Serialize(writer);
                    kvp.Cmp.Serialize(writer);
                    kvp.Value.Serialize(writer);
                }
                writer.AppendLine();
            }
        }

        public void WriteToStream(Stream stream)
        {
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                var stringBuilder = new StringBuilder();
                WriteContents(stringBuilder);
                writer.Write(stringBuilder.ToString());
            }
        }

        public static SCObject Create(List<string> tags)
        {
            SCObject res = new SCObject();

            tags.ForEach(t => res.Add(SCKeyValObject.Create(t)));
            return res;
        }

        public static SCObject Create(IEnumerable<KeyValuePair<string, string>> values)
        {
            SCObject res = new SCObject();
            res.contents.AddRange(values.Select(kvp => SCKeyValObject.Create(kvp.Key, kvp.Value)));
            return res;
        }

        public static SCObject Create(IEnumerable<SCKeyValObject> values)
        {
            SCObject res = new SCObject();
            res.contents.AddRange(values.Select(kvp => new SCKeyValObject(kvp.Key, new SCToken(Tokens.Eq), kvp.Value)));
            return res;
        }
    }

    public class SCIdentifier : SCValue
    {
        public string Name { get; }

        public SCIdentifier(string text) 
        {
            Name = text;
        }

        public override string ToString()
        {
            return Name;
        }

        internal override void Serialize(StringBuilder writer)
        {
            writer.Append(ToString());
        }

        public override bool Equals(object obj)
        {
            var other = obj as SCIdentifier;
            return other != null && other.Name.Equals(Name);
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }

    public class SCVariable : SCIdentifier
    {
        public SCVariable(string text) : base(text)
        {
        }

        public override string ToString()
        {
            return $"@{Name}";
        }
    }

    public class SCString : SCValue
    {
        public string Text { get; }

        public SCString(string text) 
        {
            Text = text?.Trim('"') ?? String.Empty;
        }

        public override string ToString()
        {
            return $"\"{Text}\"";
        }

        internal override void Serialize(StringBuilder writer)
        {
            writer.Append(ToString());
        }

        public override bool Equals(object obj)
        {
            var other = obj as SCString;
            return other!= null && other.Text.Equals(Text);
        }

        public override int GetHashCode()
        {
            return Text?.GetHashCode() ?? 0;
        }
    }

    public class SCNumber : SCValue
    {
        private readonly decimal _number;

        public SCNumber(string text) 
        {
            if (!decimal.TryParse(text, out _number))
            {
                _number = 0;
            }
        }

        public decimal Number => _number;

        public override string ToString()
        {
            return $"{_number}";
        }

        internal override void Serialize(StringBuilder writer)
        {
            writer.Append(ToString());
        }

        public override bool Equals(object obj)
        {
            var other = obj as SCNumber;
            return other != null && other._number == _number;
        }

        public override int GetHashCode()
        {
            return _number.GetHashCode();
        }
    }

    public class SCPercent : SCValue
    {
        readonly double _value; 

        public SCPercent(string text) 
        {
            var stripped = text.Replace("%", string.Empty);
            if (double.TryParse(stripped, out _value))
            {
                _value /= 100;
            }
            else
                _value = 0;
        }

        public double Value => _value; 

        public override string ToString()
        {
            return $"{_value*100}%";
        }

        internal override void Serialize(StringBuilder writer)
        {
            writer.Append(ToString());
        }

        public override bool Equals(object obj)
        {
            var other = obj as SCPercent;
            return other != null && Math.Abs(other._value - _value) < 0.00001;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }

    public class SCToken : SCValue
    {
        readonly Tokens _token;

        internal SCToken(Tokens token)
        {
            this._token = token;
        }

        public override string ToString()
        {
            switch (_token)
            {
                case Tokens.Eq: return "=";
                case Tokens.Le: return "<";
                case Tokens.Gr: return ">";
                case Tokens.LEq: return "<=";
                case Tokens.GEq: return ">=";
                case Tokens.NEq: return "!=";
                default:
                    throw new InvalidOperationException();
            }
        }

        internal override void Serialize(StringBuilder writer)
        {
            writer.Append(ToString());
        }

        public override bool Equals(object obj)
        {
            var other = obj as SCToken;
            return other != null && other._token == _token;
        }

        public override int GetHashCode()
        {
            return _token.GetHashCode();
        }
    }

}
