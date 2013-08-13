using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLXI.Compiler
{
    public class LispAstTransformer
    {
        public LispAstNode Transform(LispObject code, LispLexenv env = null)
        {
            if (env == null) env = LispLexenv.NullLexenv;
            if (code.IsSymbol)
                return TransformReference(code, env);
            return code.IsCons ? TransformCons(code, env) : TransformConstant(code, env);
        }

        private LispAstNode TransformConstant(LispObject code, LispLexenv env)
        {
            return new LispConstAstNode(env, code);
        }

        private LispAstNode TransformReference(LispObject sym, LispLexenv env)
        {
            object value;
            var var = env.GetVar(sym, out value);
            if (var == null)
            {
                return new LispGRefAstNode(env, sym);
            }
            switch (var.Kind)
            {
                case LispVarKind.Constant:
                    return new LispConstAstNode(env, (LispObject)value);
                case LispVarKind.Macro:
                    return Transform((LispObject)value, env);
                case LispVarKind.Dynamic:
                    return new LispGRefAstNode(env, sym);
                default:
                    return new LispLRefAstNode(env, var);
            }
        }

        private LispAstNode TransformCons(LispObject code, LispLexenv env)
        {
            var form = code.Car;
            if (form.Eq(LispObject.Quote))
                return TransformQuote(code, env);
            if (form.Eq(LispObject.Body))
                return TransformBody(code, env);
            if (form.Eq(LispObject.UnwindProtect))
                return TransformUnwindProtect(code, env);

            throw new NotImplementedException();
        }

        private LispAstNode TransformBody(LispObject code, LispLexenv env)
        {
            throw new NotImplementedException();
        }

        private LispAstNode TransformUnwindProtect(LispObject form, LispLexenv env)
        {
            if(!form.Cdr.IsCons || !form.Cdr.Cdr.IsProperList)
                throw new LispSyntaxException(form);
            return new LispUnwindProtectNode(
                env,
                Transform(form.ListNth(1), env),
                form.ListNthCdr(2).AsEnumerable().Select(f => Transform(f, env)));
        }

        private LispAstNode TransformQuote(LispObject form, LispLexenv env)
        {
            if(!form.Cdr.IsCons || !form.Cdr.Cdr.IsNil)
                throw new LispSyntaxException(form);
            return new LispConstAstNode(env, form.ListNth(1));
        }
    }
}
