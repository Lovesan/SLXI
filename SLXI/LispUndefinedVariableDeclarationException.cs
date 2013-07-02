using SLXI.Properties;

namespace SLXI
{
    public class LispUndefinedVariableDeclarationException : LispInvalidOperationException
    {
        public LispObject Name { get; private set; }

        public LispUndefinedVariableDeclarationException(LispObject name)
            : base(Resources.UndefinedVariableDeclarationException)
        {
            Name = name;
        }
    }
}
