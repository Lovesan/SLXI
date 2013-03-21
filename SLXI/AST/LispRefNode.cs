using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLXI.AST
{
    public abstract class LispRefNode : LispAstNode
    {
        protected LispRefNode(LispSymbol name)
        {
            Name = name;
        }

        public LispSymbol Name { get; private set; }
    }
}
