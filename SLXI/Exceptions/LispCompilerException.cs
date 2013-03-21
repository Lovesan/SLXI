using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLXI.Exceptions
{
    public class LispCompilerException : LispException
    {
        public LispCompilerException(string message)
            : base(message)
        { }
    }
}
