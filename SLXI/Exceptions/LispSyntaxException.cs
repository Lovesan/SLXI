namespace SLXI.Exceptions
{
    public class LispSyntaxException : LispCompilerException
    {
        public LispObject Form { get; private set; }

        public LispSyntaxException(LispObject form, string message)
            : base(message)
        {
            Form = form;
        }
    }
}
