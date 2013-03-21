using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLXI
{
    public class LispVector
    {
        private LispVector(IEnumerable<LispObject> elements)
        {
            Elements = new List<LispObject>(elements);
        }

        public List<LispObject> Elements { get; private set; }

        public static LispVector Create(IEnumerable<LispObject> elements)
        {
            return new LispVector(elements);
        }
    }
}
