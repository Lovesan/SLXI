using System;

namespace SLXI.Exceptions
{
    public class LispException : Exception
    {
        public LispException(string message)
            : base(message)
        { }
    }
}
