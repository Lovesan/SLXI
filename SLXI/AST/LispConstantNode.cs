using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SLXI.AST
{
    public class LispConstantNode : LispAstNode
    {
        private LispConstantNode(LispObject value)
        {
            Value = value;
        }

        public LispObject Value { get; private set; }

        public static LispConstantNode Create(LispObject value)
        {
            if(value == null)
                throw new ArgumentNullException("value");
            return new LispConstantNode(value);
        }
    }
}
