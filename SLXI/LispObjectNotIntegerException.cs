using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotIntegerException : LispTypeContraintException
    {
        public LispObjectNotIntegerException(LispObject value)
            : base(Resources.NotIntegerException, value)
        { }
    }
}
