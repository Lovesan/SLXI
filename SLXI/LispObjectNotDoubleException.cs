using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotDoubleException : LispTypeContraintException
    {
        public LispObjectNotDoubleException(LispObject value)
            : base(Resources.NotDoubleException, value)
        { }
    }
}
