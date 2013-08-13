using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLXI.Compiler
{
    public class LispGRefAstNode : LispAstNode
    {
        public LispObject Name { get; private set; }

        public LispGRefAstNode(LispLexenv env, LispObject name)
            : base(env)
        {
            name.CheckSymbol();
            Name = name;
        }
    }
}
