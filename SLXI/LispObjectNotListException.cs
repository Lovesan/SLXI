using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotListException : LispTypeContraintException
    {
        public LispObjectNotListException(LispObject value)
            : base(Resources.NotListException, value)
        { }
    }
}
