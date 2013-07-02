using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotFloatException : LispTypeContraintException
    {
        public LispObjectNotFloatException(LispObject value)
            : base(Resources.NotFloatException, value)
        { }
    }
}
