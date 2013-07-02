using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotNumberException : LispTypeContraintException
    {
        public LispObjectNotNumberException(LispObject value)
            : base(Resources.NotNumberException, value)
        { }
    }
}
