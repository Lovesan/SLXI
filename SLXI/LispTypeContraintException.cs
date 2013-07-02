namespace SLXI
{
    public class LispTypeContraintException : LispException
    {
        public LispObject Value { get; private set; }

        public LispTypeContraintException(string message, LispObject value)
            : base(message)
        {
            Value = value;
        }
    }
}
