using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotVectorException : LispTypeContraintException
    {
        public LispObjectNotVectorException(LispObject value)
            : base(Resources.NotVectorException, value)
        { }
    }
}
