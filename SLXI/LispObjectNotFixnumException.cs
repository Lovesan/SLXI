using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotFixnumException : LispTypeContraintException
    {
        public LispObjectNotFixnumException(LispObject value)
            : base(Resources.NotFixnumException, value)
        { }
    }
}
