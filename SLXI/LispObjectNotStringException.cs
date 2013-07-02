using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotStringException : LispTypeContraintException
    {
        public LispObjectNotStringException(LispObject value)
            : base(Resources.NotStringException, value)
        { }
    }
}
