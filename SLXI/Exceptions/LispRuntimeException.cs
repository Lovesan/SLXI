namespace SLXI.Exceptions
{
    public class LispRuntimeException : LispException
    {
        public LispRuntimeException(string message)
            : base(message)
        { }
    }
}
