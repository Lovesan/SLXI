using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLXI.Properties;

namespace SLXI
{
    public class LispObjectNotRealException : LispTypeContraintException
    {
        public LispObjectNotRealException(LispObject value)
            : base(Resources.NotRealException, value)
        { }
    }
}
