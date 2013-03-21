using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLXI.AST
{
    public class LispURefNode : LispAstNode
    {
        private LispURefNode(LispSymbol name)
        {
            Name = name;
        }

        public LispSymbol Name { get; private set; }

        public static LispURefNode Create(LispSymbol name)
        {
            return new LispURefNode(name);
        }
    }
}
