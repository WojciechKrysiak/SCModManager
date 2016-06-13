using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCModManager.SCFormat
{
    internal partial class Parser
    {
        List<SCKeyValObject> _currentValues = new List<SCKeyValObject>();
        Stack<SCObject> _currentObjectStack = new Stack<SCObject>();

        public SCObject Root
        {
            get
            {
                return _currentObjectStack.Peek();
            }
        }

        public bool ParseError => _currentObjectStack.Count > 1;

        public Parser(Scanner scnr) : base(scnr)
        {
            _currentObjectStack.Push(new SCObject());
        }

        void SetKeyValue(SCValue key, SCValue cmp, SCValue value)
        {
            _currentObjectStack.Peek().Add(key, cmp, value);
        }

        void PushNewObject(SCValue name)
        {
            _currentObjectStack.Push(new SCObject(name as SCIdentifier));
        }

        SCValue PopObject()
        {
            return _currentObjectStack.Pop();
        }
    }
}
