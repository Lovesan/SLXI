using System;
using System.Collections.Generic;
using SLXI.Exceptions;
namespace SLXI
{
    public static class LispObjectExtensions
    {
        private static readonly LispSymbol Nil = LispSymbol.Nil; 

        private static Func<LispException> CurrentExceptionFactory
        {
            get
            {
                return  LispRuntime.Current.GetValue(LispSymbol.SlxiCurrentExceptionFactory)
                    .As<LispInternalObject<Func<LispException>>>(() => new LispRuntimeException("Internal runtime exception"))
                    .Value;
            }
        }

        public static bool Eq(this LispObject obj, LispObject other)
        {
            return ReferenceEquals(obj, other);
        }

        public static bool IsNil(this LispObject obj)
        {
            return obj.Eq(Nil);
        }

        public static bool IsCons(this LispObject obj)
        {
            return obj is LispCons;
        }

        public static bool IsSymbol(this LispObject obj)
        {
            return obj is LispSymbol;
        }

        public static bool IsEnd(this LispObject obj, Func<LispException> exFactory)
        {
            return obj.AsList(exFactory).IsNil();
        }

        public static bool IsEnd(this LispObject obj)
        {
            return obj.IsEnd(CurrentExceptionFactory);
        }
        
        public static T As<T>(this LispObject obj, Func<LispException> exFactory)
            where T : LispObject
        {
            var t = obj as T;
            if (t == null)
                throw exFactory();
            return t;
        }

        public static T As<T>(this LispObject obj)
            where T : LispObject
        {
            return obj.As<T>(CurrentExceptionFactory);
        }

        public static T As<T>(this LispObject obj, T otherObject, Func<LispException> exFactory)
            where T : LispObject
        {
            if (obj.Eq(otherObject))
                return otherObject;
            throw exFactory();
        }

        public static T As<T>(this LispObject obj, T otherObject)
            where T : LispObject
        {
            return obj.As(otherObject, CurrentExceptionFactory);
        }

        public static LispSymbol AsNil(this LispObject obj, Func<LispException> exFactory)
        {
            if (obj.Eq(Nil))
                return Nil;
            throw exFactory();
        }

        public static LispSymbol AsNil(this LispObject obj)
        {
            return obj.AsNil(CurrentExceptionFactory);
        }

        public static LispSymbol AsSymbol(this LispObject obj, Func<LispException> exFactory)
        {
            var s = obj as LispSymbol;
            if (s == null)
                throw exFactory();
            return s;
        }

        public static LispSymbol AsSymbol(this LispObject obj)
        {
            return obj.AsSymbol(CurrentExceptionFactory);
        }

        public static LispCons AsCons(this LispObject obj, Func<LispException> exFactory)
        {
            var cons = obj as LispCons;
            if (cons == null)
                throw exFactory();
            return cons;
        }

        public static LispCons AsCons(this LispObject obj)
        {
            return obj.AsCons(CurrentExceptionFactory);
        }

        public static LispObject AsList(this LispObject obj, Func<LispException> exFactory)
        {
            if (obj.IsNil()) return Nil;
            return obj.AsCons(exFactory);
        }

        public static LispObject AsList(this LispObject obj)
        {
            return obj.AsList(CurrentExceptionFactory);
        }

        public static List<LispObject> AsList(this LispObject obj, int size, Func<LispException> exFactory)
        {
            var finite = size >= 0;
            var bounded = finite && size != int.MaxValue;
            var r = new List<LispObject>();
            var list = obj.AsList(exFactory);
            while(!list.IsNil())
            {
                if (finite)
                {
                    --size;
                    if (size < 0) throw exFactory();
                }
                r.Add(list.First(exFactory));
                list = list.Rest(exFactory);
            }
            if (bounded && size != 0)
                throw exFactory();
            return r;
        }

        public static List<LispObject> AsList(this LispObject obj, int size)
        {
            return obj.AsList(size, CurrentExceptionFactory);
        }

        public static List<LispObject> AsDotted(this LispObject obj, int size, out LispObject last, Func<LispException> exFactory)
        {
            var finite = size >= 0;
            var bounded = finite && size != int.MaxValue;
            var r = new List<LispObject>();
            var list = obj.AsList(exFactory);
            while(list.IsCons())
            {
                if (finite)
                {
                    --size;
                    if (size < 0) throw exFactory();
                }
                r.Add(list.First(exFactory));
                list = list.Rest(exFactory);
            }
            if (bounded && size != 0)
                throw exFactory();
            last = list;
            return r;
        }

        public static List<LispObject> AsDotted(this LispObject obj, int size, out LispObject last)
        {
            return obj.AsDotted(size, out last, CurrentExceptionFactory);
        }

        public static T First<T>(this LispObject obj, Func<LispException> exFactory)
            where T : LispObject
        {
            return obj.IsNil() ? Nil.As<T>(exFactory) : obj.AsCons(exFactory).Car.As<T>(exFactory);
        }

        public static T First<T>(this LispObject obj)
            where T : LispObject
        {
            return obj.First<T>(CurrentExceptionFactory);
        }

        public static LispObject First(this LispObject obj, Func<LispException> exFactory)
        {
            return obj.First<LispObject>(exFactory);
        }

        public static LispObject First(this LispObject obj)
        {
            return obj.First(CurrentExceptionFactory);
        }

        public static T Second<T>(this LispObject obj, Func<LispException> exFactory)
            where T : LispObject
        {
            return obj.IsNil() ? Nil.As<T>(exFactory) : obj.AsCons(exFactory).Cdr.First<T>(exFactory);
        }

        public static LispObject Second(this LispObject obj, Func<LispException> exFactory)
        {
            return obj.Second<LispObject>(exFactory);
        }

        public static T Second<T>(this LispObject obj)
            where T : LispObject
        {
            var exFactory = CurrentExceptionFactory;
            return obj.IsNil() ? Nil.As<T>(exFactory) : obj.AsCons(exFactory).Cdr.First<T>(exFactory);
        }

        public static LispObject Second(this LispObject obj)
        {
            return obj.Second<LispObject>(CurrentExceptionFactory);
        }

        public static T Third<T>(this LispObject obj, Func<LispException> exFactory)
            where T : LispObject
        {
            return obj.IsNil() ? Nil.As<T>(exFactory) : obj.AsCons(exFactory).Cdr.Second<T>(exFactory);
        }

        public static LispObject Third(this LispObject obj, Func<LispException> exFactory)
        {
            return obj.Third<LispObject>(exFactory);
        }

        public static T Third<T>(this LispObject obj)
            where T : LispObject
        {
            var exFactory = CurrentExceptionFactory;
            return obj.IsNil() ? Nil.As<T>(exFactory) : obj.AsCons(exFactory).Cdr.Second<T>(exFactory);
        }

        public static LispObject Third(this LispObject obj)
        {
            return obj.Third<LispObject>(CurrentExceptionFactory);
        }

        public static T Rest<T>(this LispObject obj, Func<LispException> exFactory)
            where T : LispObject
        {
            return obj.IsNil() ? Nil.As<T>(exFactory) : obj.AsCons(exFactory).Cdr.As<T>(exFactory);
        }

        public static LispObject Rest(this LispObject obj, Func<LispException> exFactory)
        {
            return obj.Rest<LispObject>(exFactory);
        }

        public static T Rest<T>(this LispObject obj)
            where T : LispObject
        {
            var exFactory = CurrentExceptionFactory;
            return obj.IsNil() ? Nil.As<T>(exFactory) : obj.AsCons(exFactory).Cdr.As<T>(exFactory);
        }

        public static LispObject Rest(this LispObject obj)
        {
            return obj.Rest<LispObject>(CurrentExceptionFactory);
        }

        public static LispObject List(this IEnumerable<LispObject> list)
        {
            var i = list.GetEnumerator();
            if (!i.MoveNext())
                return LispSymbol.Nil;
            var head = LispCons.Create(i.Current, LispSymbol.Nil);
            if (!i.MoveNext())
                return head;
            var tail = head;
            do
            {
                var tmp = LispCons.Create(i.Current, LispSymbol.Nil);
                tail.Cdr = tmp;
                tail = tmp;
            } while (i.MoveNext());
            return head;
        }
    }
}
