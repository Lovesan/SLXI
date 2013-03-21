namespace SLXI
{
    public class LispCons : LispObject
    {
        private LispCons(LispObject car, LispObject cdr)
        {
            Car = car;
            Cdr = cdr;
        }

        public LispObject Car { get; set; }

        public LispObject Cdr { get; set; }

        public static LispCons Create(LispObject car, LispObject cdr)
        {
            return new LispCons(car, cdr);
        }
    }
}
