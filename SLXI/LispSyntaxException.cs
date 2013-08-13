using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLXI.Properties;

namespace SLXI
{
    public class LispSyntaxException : LispException
    {
        public LispObject Form { get; private set; }

        public LispSyntaxException(string message, LispObject form)
            : base(message)
        {
            Form = form;
        }

        public LispSyntaxException(LispObject form)
            : this(Resources.InvalidSyntaxException, form)
        { }
    }
}
