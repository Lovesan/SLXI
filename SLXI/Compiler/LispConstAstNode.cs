using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLXI.Compiler
{
    public class LispConstAstNode : LispAstNode
    {
        public LispObject Value { get; private set; }

        public LispConstAstNode(LispLexenv env, LispObject val)
            : base(env)
        {
            Value = val;
        }
    }
}
