namespace SLXI
{
    public class LispInternalObject<T> : LispObject
    {
        private LispInternalObject(T value)
        {
            Value = value;
        }

        public T Value { get; private set; }

        public static LispInternalObject<T> Create(T value)
        {
            return new LispInternalObject<T>(value);
        }
    }
}
