using System;

namespace SLXI
{
    public class LispException : Exception
    {
        public LispException(string message)
            : base(message)
        { }
    }
}
