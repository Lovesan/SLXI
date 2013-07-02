using SLXI.Properties;

namespace SLXI
{
    public class LispRangeContraintException : LispConstraintException
    {
        public LispObject Value { get; private set; }

        public LispObject Start { get; private set; }

        public LispObject End { get; private set; }

        public LispRangeContraintException(LispObject value, LispObject start, LispObject end)
            : base(Resources.RangeConstraintException)
        {
            Value = value;
            Start = start;
            End = end;
        }
    }
}
