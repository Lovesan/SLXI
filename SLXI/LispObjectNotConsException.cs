using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotConsException : LispTypeContraintException
    {
        public LispObjectNotConsException(LispObject value)
            : base(Resources.NotConsException, value)
        { }
    }
}
