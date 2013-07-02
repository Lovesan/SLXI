using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotCharException : LispTypeContraintException
    {
        public LispObjectNotCharException(LispObject value)
            : base(Resources.NotCharException, value)
        { }
    }
}
