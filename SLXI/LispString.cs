using System.Collections.Generic;

namespace SLXI
{
    public class LispString : LispObject
    {
        private LispString(IEnumerable<char> value)
        {
            Value = new List<char>(value);
        }

        public List<char> Value { get; private set; }

        public string StringValue
        {
            get { return new string(Value.ToArray()); }
        }

        public LispChar this[int index]
        {
            get { return LispChar.Create(Value[index]); }
            set { Value[index] = value.Value; }
        }

        public static LispString Create(IEnumerable<char> value)
        {
            return new LispString(value);
        }
    }
}
