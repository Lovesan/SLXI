using SLXI.Properties;

namespace SLXI
{
    public class LispSymbolUnboundException : LispInvalidOperationException
    {
        public LispObject Symbol { get; private set; }

        public LispSymbolUnboundException(LispObject symbol)
            : base(Resources.SymbolUnboundException)
        {
            Symbol = symbol;
        }
    }
}
