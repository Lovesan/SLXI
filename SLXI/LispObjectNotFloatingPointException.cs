using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotFloatingPointException : LispTypeContraintException
    {
        public LispObjectNotFloatingPointException(LispObject value)
            : base(Resources.NotFloatingPointException, value)
        { }
    }
}
