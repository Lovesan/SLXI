using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLXI.Compiler
{
    public class LispLRefAstNode : LispAstNode
    {
        public LispVar Var { get; private set; }

        public LispLRefAstNode(LispLexenv env, LispVar var)
            : base(env)
        {
            Var = var;
        }
    }
}
