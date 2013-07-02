namespace SLXI
{
    public class LispInvalidOperationException : LispException
    {
        public LispInvalidOperationException(string message)
            : base(message)
        { }
    }
}
