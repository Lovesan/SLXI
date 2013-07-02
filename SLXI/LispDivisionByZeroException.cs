using SLXI.Properties;

namespace SLXI
{
    public class LispDivisionByZeroException : LispException
    {
        public LispObject Arg { get; private set; }

        public LispDivisionByZeroException(LispObject arg)
            : base(Resources.DivisionByZeroException)
        {
            Arg = arg;
        }
    }
}
