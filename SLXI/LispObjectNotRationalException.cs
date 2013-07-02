using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotRationalException : LispTypeContraintException
    {
        public LispObjectNotRationalException(LispObject value)
            : base(Resources.NotRationalException, value)
        { }
    }
}
