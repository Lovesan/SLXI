using System;

namespace SLXI.Exceptions
{
    public class LispUnboundVariableException : LispUnboundException
    {
        public LispUnboundVariableException(LispSymbol name, string message)
            : base(message)
        {
            if(name == null)
                throw new ArgumentNullException("name");
            Name = name;
        }

        public LispUnboundVariableException(LispSymbol name)
            : this(name, "Variable " + name.Name.StringValue + " is unbound")
        { }

        public LispSymbol Name { get; private set; }
    }
}
