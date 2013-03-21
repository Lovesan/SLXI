using System;

namespace SLXI.Exceptions
{
    public class LispConstantModificationException : LispRuntimeException
    {
        public LispConstantModificationException(LispSymbol name, string message)
            : base(message)
        {
            if(name == null)
                throw new ArgumentNullException("name");
            Name = name;
        }

        public LispConstantModificationException(LispSymbol name)
            : this(name, "Unable to modify constant " + name.Name.StringValue)
        { }

        public LispSymbol Name { get; private set; }
    }
}
