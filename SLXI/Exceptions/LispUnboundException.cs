namespace SLXI.Exceptions
{
    public class LispUnboundException : LispRuntimeException
    {
        public LispUnboundException(string message)
            : base(message)
        { }
    }
}
