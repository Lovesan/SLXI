using System.Collections.Generic;
using System.Linq;

namespace SLXI
{
    public class LispValues : List<LispObject>
    {
        public LispValues()
        { }

        public LispValues(IEnumerable<LispObject> values)
            : base(values ?? Enumerable.Empty<LispObject>())
        { }
    }
}
