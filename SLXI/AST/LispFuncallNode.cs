using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace SLXI.AST
{
    public class LispFuncallNode : LispAstNode
    {
        private LispFuncallNode(LispAstNode function, IEnumerable<LispAstNode> args, LispAstNode rest)
        {
            Function = function;
            Args = args;
            Rest = rest;
        }

        public LispAstNode Function { get; private set; }

        public IEnumerable<LispAstNode> Args { get; private set; }

        public LispAstNode Rest { get; private set; }

        public static LispFuncallNode Create(LispAstNode function, IEnumerable<LispAstNode> args, LispAstNode rest = null)
        {
            if(function == null)
                throw new ArgumentNullException("function");
            if(args == null)
                throw new ArgumentNullException("args");
            return new LispFuncallNode(function, args, rest);
        }
    }
}
