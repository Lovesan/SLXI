using SLXI.Properties;

namespace SLXI
{
    public class LispVariableRedeclarationException : LispInvalidOperationException
    {
        public LispObject Name { get; private set; }

        public LispVariableRedeclarationException(LispObject name)
            : base(Resources.VariableRedeclarationException)
        {
            Name = name;
        }
    }
}
