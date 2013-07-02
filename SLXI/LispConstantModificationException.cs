using SLXI.Properties;

namespace SLXI
{
    public class LispConstantModificationException : LispInvalidOperationException
    {
        public LispObject Name { get; private set; }

        public LispConstantModificationException(LispObject name)
            : base(name.Eq(LispObject.T)
                    ? Resources.TModificationException
                    : (name.Eq(LispObject.Nil)
                        ? Resources.NilModificationException
                        : Resources.ConstantModificationException))
        {
            Name = name;
        }
    }
}
