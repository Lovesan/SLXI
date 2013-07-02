using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotSequenceException : LispTypeContraintException
    {
        public LispObjectNotSequenceException(LispObject value)
            : base(Resources.NotSequenceException, value)
        { }
    }
}
