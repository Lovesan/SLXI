using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLXI
{
    public class LispChar : LispObject
    {
        private LispChar(char c)
        {
            Value = c;
        }

        public Char Value { get; private set; }

        public static LispChar Create(char c)
        {
            return new LispChar(c);
        }
    }
}
