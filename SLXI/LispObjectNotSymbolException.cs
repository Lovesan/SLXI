using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotSymbolException : LispTypeContraintException
    {
        public LispObjectNotSymbolException(LispObject value)
            : base(Resources.NotSymbolException, value)
        { }
    }
}
