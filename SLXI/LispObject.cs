using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using SLXI.Compiler;

namespace SLXI
{
    public struct LispObject
    {
        #region Delegate disposable

        class DelegateDisposable : IDisposable
        {
            private readonly Action _action;

            public DelegateDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                if (_action != null)
                    _action();
            }
        }

        #endregion

        #region Data classes

        interface IConsData
        {
            LispObject Car { get; set; }

            LispObject Cdr { get; set; }
        }

        class ConsData : IConsData
        {
            public LispObject Car { get; set; }

            public LispObject Cdr { get; set; }

            public override bool Equals(object obj)
            {
                var cons = obj as ConsData;
                return cons != null && Equals(cons);
            }

            public bool Equals(ConsData data)
            {
                return Car.Equals(data.Car) && Cdr.Equals(data.Cdr);
            }
        }

        interface ISymbolData
        {
            LispObject? Parent { get; set; }

            Dictionary<StringData, LispObject> Children { get; set; }

            StringData Name { get; }

            LispObject? GlobalValue { get; set; }

            ThreadLocal<LispObject?> LocalValue { get; set; }
        }

        class SymbolData : ISymbolData
        {
            private readonly int _hashCode;

            public SymbolData(StringData name)
            {
                Name = name;
                _hashCode = name.GetHashCode();
            }

            public LispObject? Parent { get; set; }

            public Dictionary<StringData, LispObject> Children { get; set; }

            public StringData Name { get; private set; }

            public LispObject? GlobalValue { get; set; }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                return ReferenceEquals(this, obj);
            }


            public ThreadLocal<LispObject?> LocalValue { get; set; }
        }

        class NilData : SymbolData, IConsData
        {
            public LispObject Car { get; set; }

            public LispObject Cdr { get; set; }

            public NilData()
                : base(StringData.FromString("nil"))
            { }
        }

        class TData : SymbolData
        {
            public TData()
                : base(StringData.FromString("t"))
            { }
        }

        class StringData
        {
            public int[] Value { get; set; }

            public override int GetHashCode()
            {
                return Value.Aggregate(31, (current, t) => current * 31 + t);
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                var s = obj as StringData;
                if (s == null) return false;
                return Equals(s);
            }

            public bool Equals(StringData s)
            {
                if (s.Value.Length != Value.Length) return false;
                return !Value.Where((t, i) => t != s.Value[i]).Any();
            }

            public StringData Copy()
            {
                var a = new int[Value.Length];
                Value.CopyTo(a, 0);
                return new StringData { Value = a };
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                foreach (var c in Value)
                    sb.Append(char.ConvertFromUtf32(c));
                return sb.ToString();
            }

            public static StringData FromString(string s)
            {
                var i = StringInfo.GetTextElementEnumerator(s);
                var l = new List<int>();
                while (i.MoveNext())
                {
                    l.Add(char.ConvertToUtf32(i.GetTextElement(), 0));
                }
                return new StringData
                    {
                        Value = l.ToArray()
                    };
            }
        }

        class RatioFixnumData
        {
            public int Numerator { get; set; }

            public int Denominator { get; set; }


            public override int GetHashCode()
            {
                return Numerator.GetHashCode() ^ Denominator.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var ratio = obj as RatioFixnumData;
                if (ratio == null) return false;
                return Equals(ratio);
            }

            public bool Equals(RatioFixnumData r)
            {
                return Numerator == r.Numerator && Denominator == r.Denominator;
            }
        }

        class RatioIntegerData
        {
            public BigInteger Numerator { get; set; }

            public BigInteger Denominator { get; set; }

            public override int GetHashCode()
            {
                return Numerator.GetHashCode() ^ Denominator.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                var ratio = obj as RatioIntegerData;
                if (ratio == null) return false;
                return Equals(ratio);
            }

            public bool Equals(RatioIntegerData r)
            {
                return Numerator == r.Numerator && Denominator == r.Denominator;
            }
        }

        #endregion

        #region Static fields & constructor

        private static readonly ConcurrentDictionary<LispObject, LispVar> Vars;

        private static readonly LispObject TSymbol;
        private static readonly LispObject NilSymbol;
        private static readonly NilData NilDataValue;
        private static readonly TData TDataValue;
        private static readonly LispObject UnboundMarker;
        private static readonly ConsData UnboundMarkerValue;

        public static LispObject T
        {
            get { return TSymbol; }
        }

        public static LispObject Nil
        {
            get { return NilSymbol; }
        }

        public static readonly LispObject Quote;
        public static readonly LispObject UnwindProtect;
        public static readonly LispObject Body;

        static LispObject()
        {
            NilSymbol = new LispObject(LispObjectType.Symbol);
            NilDataValue = new NilData { Car = Nil, Cdr = Nil, GlobalValue = Nil };
            TDataValue = new TData();
            TSymbol = new LispObject(LispObjectType.Symbol, TDataValue);
            TDataValue.GlobalValue = T;
            TDataValue.Parent = Nil;
            TDataValue.Children = new Dictionary<StringData, LispObject> { { NilDataValue.Name, Nil } };
            NilDataValue.Parent = T;
            NilDataValue.Children = new Dictionary<StringData, LispObject> { { TDataValue.Name, T } };
            UnboundMarkerValue = new ConsData { Car = Nil, Cdr = Nil };
            UnboundMarker = new LispObject(LispObjectType.Cons, UnboundMarkerValue);
            Vars = new ConcurrentDictionary<LispObject, LispVar>(new Dictionary<LispObject, LispVar>
                {
                    { T, new LispVar(T, LispVarKind.Constant) },
                    { Nil, new LispVar(Nil, LispVarKind.Constant) }
                });
            Quote = T.SymbolIntern("quote");
            UnwindProtect = T.SymbolIntern("unwind-protect");
            Body = T.SymbolIntern("body");
        }

        #endregion

        #region Fields

        private readonly LispObjectType _type;
        private readonly object _data;

        #endregion

        #region Private constructor

        private LispObject(LispObjectType type, object data = null)
        {
            _type = type;
            _data = data;
        }

        #endregion

        #region Properties

        public LispObjectType Type
        {
            get { return _type; }
        }

        #endregion

        #region Constructors

        public static LispObject CreateSymbol(string name)
        {
            return new LispObject(
                LispObjectType.Symbol,
                new SymbolData(StringData.FromString(name)));
        }

        public static LispObject CreateSymbol(LispObject name)
        {
            name.CheckString();
            return new LispObject(
                LispObjectType.Symbol,
                new SymbolData(name.GetData<StringData>().Copy()));
        }

        public static LispObject CreateCons(LispObject car, LispObject cdr)
        {
            return new LispObject(LispObjectType.Cons, new ConsData { Car = car, Cdr = cdr });
        }

        public static LispObject CreateString(string s)
        {
            return new LispObject(
                LispObjectType.String,
                StringData.FromString(s));
        }

        public static LispObject CreateString(int length)
        {
            return new LispObject(
                LispObjectType.String,
                new StringData { Value = new int[length] });
        }

        public static LispObject CreateString(IEnumerable<int> characters)
        {
            return new LispObject(
                LispObjectType.String,
                new StringData { Value = (characters ?? Enumerable.Empty<int>()).ToArray() });
        }

        public static LispObject CreateChar(int c)
        {
            return new LispObject(LispObjectType.Char, c);
        }

        public static LispObject CreateVector(IEnumerable<LispObject> objects)
        {
            return new LispObject(LispObjectType.Vector, (objects ?? Enumerable.Empty<LispObject>()).ToArray());
        }

        public static LispObject CreateVector(int length)
        {
            return new LispObject(LispObjectType.Vector, Enumerable.Repeat(Nil, length).ToArray());
        }

        public static LispObject CreateFixnum(int value)
        {
            return new LispObject(LispObjectType.Fixnum, value);
        }

        public static LispObject CreateInteger(BigInteger value)
        {
            return (value > int.MaxValue || value < int.MinValue)
                ? new LispObject(LispObjectType.Bignum, value)
                : new LispObject(LispObjectType.Fixnum, (int)value);
        }

        public static LispObject CreateFloat(float value)
        {
            return new LispObject(LispObjectType.Float, value);
        }

        public static LispObject CreateDouble(double value)
        {
            return new LispObject(LispObjectType.Double, value);
        }

        public static LispObject CreateRatio(BigInteger num, BigInteger denom)
        {
            if (denom.IsZero)
                throw new LispDivisionByZeroException(CreateInteger(num));
            var sign = num.Sign * denom.Sign;
            num = BigInteger.Abs(num);
            denom = BigInteger.Abs(denom);
            var gcd = BigInteger.GreatestCommonDivisor(num, denom);
            num /= gcd;
            denom /= gcd;
            if (denom.IsOne)
                return CreateInteger(num * sign);
            if (num > int.MaxValue || num < int.MinValue || denom > int.MaxValue || denom < int.MinValue)
                return new LispObject(LispObjectType.RatioInteger,
                                      new RatioIntegerData { Numerator = num, Denominator = denom });
            return new LispObject(LispObjectType.RatioFixnum,
                                  new RatioFixnumData { Numerator = (int)num, Denominator = (int)denom });
        }

        #endregion

        #region Type checkers & predicates

        public bool IsNil
        {
            get { return _data == null; }
        }

        public bool IsT
        {
            get { return ReferenceEquals(_data, TDataValue); }
        }

        public bool IsCons
        {
            get { return _type == LispObjectType.Cons; }
        }

        public bool IsList
        {
            get { return IsNil || IsCons; }
        }

        public bool IsAtom
        {
            get { return !IsList; }
        }

        public bool IsSymbol
        {
            get { return _type == LispObjectType.Symbol; }
        }

        public bool IsString
        {
            get { return _type == LispObjectType.String; }
        }

        public bool IsVector
        {
            get { return _type == LispObjectType.Vector; }
        }

        public bool IsSequence
        {
            get { return IsList || IsString || IsVector; }
        }

        public bool IsChar
        {
            get { return _type == LispObjectType.Char; }
        }

        public bool IsFixnum
        {
            get { return _type == LispObjectType.Fixnum; }
        }

        public bool IsBignum
        {
            get { return _type == LispObjectType.Bignum; }
        }

        public bool IsInteger
        {
            get { return IsFixnum || IsBignum; }
        }

        public bool IsRatioFixnum
        {
            get { return _type == LispObjectType.RatioFixnum; }
        }

        public bool IsRatioInteger
        {
            get { return _type == LispObjectType.RatioInteger; }
        }

        public bool IsRatio
        {
            get { return IsRatioFixnum || IsRatioInteger; }
        }

        public bool IsFloat
        {
            get { return _type == LispObjectType.Float; }
        }

        public bool IsDouble
        {
            get { return _type == LispObjectType.Double; }
        }

        public bool IsRational
        {
            get { return IsInteger || IsRatio; }
        }

        public bool IsFloatingPoint
        {
            get { return IsFloat || IsDouble; }
        }

        public bool IsReal
        {
            get { return IsRational || IsFloatingPoint; }
        }

        public bool IsNumber
        {
            get { return IsReal; }
        }

        public bool IsFunction
        {
            get { return _type == LispObjectType.Function; }
        }

        public void CheckSymbol()
        {
            if (!IsSymbol)
                throw new LispObjectNotSymbolException(this);
        }

        public void CheckList()
        {
            if (!IsList)
                throw new LispObjectNotListException(this);
        }

        public void CheckCons()
        {
            if (!IsCons)
                throw new LispObjectNotConsException(this);
        }

        public void CheckChar()
        {
            if (!IsChar)
                throw new LispObjectNotCharException(this);
        }

        public void CheckString()
        {
            if (!IsString)
                throw new LispObjectNotStringException(this);
        }

        public void CheckVector()
        {
            if (!IsVector)
                throw new LispObjectNotVectorException(this);
        }

        public void CheckSequence()
        {
            if (!IsSequence)
                throw new LispObjectNotSequenceException(this);
        }

        public void CheckNumber()
        {
            if (!IsNumber)
                throw new LispObjectNotNumberException(this);
        }

        public void CheckRational()
        {
            if (!IsRational)
                throw new LispObjectNotRationalException(this);
        }

        public void CheckInteger()
        {
            if (!IsInteger)
                throw new LispObjectNotIntegerException(this);
        }

        public void CheckFixnum()
        {
            if (!IsFixnum)
                throw new LispObjectNotFixnumException(this);
        }

        public void CheckFloat()
        {
            if (!IsFloat)
                throw new LispObjectNotFloatException(this);
        }

        public void CheckDouble()
        {
            if (!IsDouble)
                throw new LispObjectNotDoubleException(this);
        }

        public void CheckFloatingPoint()
        {
            if (!IsFloatingPoint)
                throw new LispObjectNotFloatingPointException(this);
        }

        #endregion

        #region Data accessor

        private T GetData<T>()
        {
            return (T)(_data ?? NilDataValue);
        }

        #endregion

        #region Primitive accessors

        public LispObject Car
        {
            get
            {
                CheckList();
                return GetData<IConsData>().Car;
            }
            set
            {
                CheckCons();
                GetData<IConsData>().Car = value;
            }
        }

        public LispObject Cdr
        {
            get
            {
                CheckList();
                return GetData<IConsData>().Cdr;
            }
            set
            {
                CheckCons();
                GetData<IConsData>().Cdr = value;
            }
        }

        public int CharValue
        {
            get
            {
                CheckChar();
                return GetData<int>();
            }
        }

        public string StringValue
        {
            get
            {
                CheckString();
                return GetData<StringData>().ToString();
            }
        }

        public int StringLength
        {
            get
            {
                CheckString();
                return GetData<StringData>().Value.Length;
            }
        }

        public LispObject StringRef(int i)
        {
            CheckString();
            var data = GetData<StringData>().Value;
            if (i < 0 || i >= data.Length)
                throw new LispRangeContraintException(
                    CreateInteger(i),
                    CreateInteger(0),
                    CreateInteger(data.Length - 1));
            return CreateChar(data[i]);
        }

        public void StringSet(int i, LispObject value)
        {
            CheckString();
            var data = GetData<StringData>().Value;
            if (i < 0 || i >= data.Length)
                throw new LispRangeContraintException(
                    CreateInteger(i),
                    CreateInteger(0),
                    CreateInteger(data.Length - 1));
            data[i] = value.CharValue;
        }

        public int VectorLength
        {
            get
            {
                CheckVector();
                return GetData<LispObject[]>().Length;
            }
        }

        public LispObject VectorRef(int index)
        {
            CheckVector();
            var data = GetData<LispObject[]>();
            if (index < 0 || index >= data.Length)
                throw new LispRangeContraintException(
                    CreateInteger(index),
                    CreateInteger(0),
                    CreateInteger(data.Length - 1));
            return data[index];
        }

        public void VectorSet(int index, LispObject value)
        {
            CheckVector();
            var data = GetData<LispObject[]>();
            if (index < 0 || index >= data.Length)
                throw new LispRangeContraintException(
                    CreateInteger(index),
                    CreateInteger(0),
                    CreateInteger(data.Length - 1));
            data[index] = value;
        }

        public BigInteger IntegerValue
        {
            get
            {
                CheckInteger();
                return IsFixnum ? GetData<int>() : GetData<BigInteger>();
            }
        }

        public int FixnumValue
        {
            get
            {
                CheckFixnum();
                return GetData<int>();
            }
        }

        public float FloatValue
        {
            get
            {
                CheckFloat();
                return GetData<float>();
            }
        }

        public double DoubleValue
        {
            get
            {
                CheckDouble();
                return GetData<double>();
            }
        }

        public double FloatingPointValue
        {
            get
            {
                CheckFloatingPoint();
                return IsFloat ? GetData<float>() : GetData<double>();
            }
        }

        public BigInteger Numerator
        {
            get
            {
                CheckRational();
                if (IsInteger) return GetData<BigInteger>();
                return IsRatioFixnum
                    ? GetData<RatioFixnumData>().Numerator
                    : GetData<RatioIntegerData>().Numerator;
            }
        }

        public LispObject NumeratorValue
        {
            get
            {
                CheckRational();
                if (IsInteger) return CreateInteger(GetData<BigInteger>());
                return IsRatioFixnum
                           ? CreateFixnum(GetData<RatioFixnumData>().Numerator)
                           : CreateInteger(GetData<RatioIntegerData>().Numerator);
            }
        }

        public BigInteger Denominator
        {
            get
            {
                CheckRational();
                if (IsInteger) return 1;
                return IsRatioFixnum
                    ? GetData<RatioFixnumData>().Denominator
                    : GetData<RatioIntegerData>().Denominator;
            }
        }

        public LispObject DenominatorValue
        {
            get
            {
                CheckRational();
                if (IsInteger) return CreateInteger(1);
                return IsRatioFixnum
                           ? CreateFixnum(GetData<RatioFixnumData>().Denominator)
                           : CreateInteger(GetData<RatioIntegerData>().Denominator);
            }
        }

        public LispObject SymbolName
        {
            get
            {
                CheckSymbol();
                return CreateString(GetData<ISymbolData>().Name.Value);
            }
        }

        public LispObject SymbolGlobalValue
        {
            get
            {
                CheckSymbol();
                var data = GetData<ISymbolData>();
                if (data.GlobalValue.HasValue)
                    return data.GlobalValue.Value;
                throw new LispSymbolUnboundException(this);
            }
            set
            {
                CheckSymbol();
                if (IsT || IsNil) throw new LispConstantModificationException(this);
                var data = GetData<ISymbolData>();
                lock (data)
                {
                    data.GlobalValue = value;
                }
            }
        }

        public bool IsSymbolGloballyBound
        {
            get
            {
                CheckSymbol();
                return GetData<ISymbolData>().GlobalValue.HasValue;
            }
        }

        public void UnbindSymbolGlobalValue()
        {
            CheckSymbol();
            var data = GetData<ISymbolData>();
            lock (data)
            {
                data.GlobalValue = null;
            }
        }

        public bool TryGetSymbolParent(out LispObject parent)
        {
            CheckSymbol();
            var p = GetData<ISymbolData>().Parent;
            if (p.HasValue)
            {
                parent = p.Value;
                return true;
            }
            parent = Nil;
            return false;
        }

        public bool TryGetSymbolChild(LispObject name, out LispObject child)
        {
            CheckSymbol();
            name.CheckString();
            var data = GetData<ISymbolData>();
            if (data.Children == null)
            {
                child = Nil;
                return false;
            }
            return data.Children.TryGetValue(name.GetData<StringData>(), out child);
        }

        #endregion

        #region Symbol operations

        public void CheckNotConstantSymbol()
        {
            LispVar var;
            if (Vars.TryGetValue(this, out var) && var.Kind == LispVarKind.Constant)
                throw new LispConstantModificationException(this);
        }

        public LispObject SymbolIntern(LispObject name)
        {
            CheckSymbol();
            name.CheckString();
            var data = GetData<ISymbolData>();
            var str = name.GetData<StringData>();
            lock (data)
            {
                if (data.Children == null)
                    data.Children = new Dictionary<StringData, LispObject>();
                LispObject child;
                if (data.Children.TryGetValue(str, out child))
                    return child;
                child = CreateSymbol(name);
                child.GetData<ISymbolData>().Parent = this;
                data.Children[str] = child;
                return child;
            }
        }

        public LispObject SymbolIntern(string name)
        {
            return SymbolIntern(CreateString(name));
        }

        public bool SymbolUnintern()
        {
            CheckSymbol();
            if (IsT || IsNil) throw new LispConstantModificationException(this);
            var data = GetData<ISymbolData>();
            if (!data.Parent.HasValue) return false;
            lock (data)
            {
                var parentData = data.Parent.Value.GetData<ISymbolData>();
                lock (parentData)
                {
                    parentData.Children.Remove(data.Name);
                    data.Parent = null;
                    return true;
                }
            }
        }

        public LispObject SymbolChildren
        {
            get
            {
                CheckSymbol();
                var data = GetData<ISymbolData>();
                if (data.Children == null) return Nil;
                lock (data)
                {
                    return CreateList(data.Children.Values.ToArray());
                }
            }
        }

        public LispObject SymbolValue
        {
            get
            {
                CheckSymbol();
                var data = GetData<ISymbolData>();
                if (data.LocalValue != null && data.LocalValue.Value.HasValue)
                {
                    if (data.LocalValue.Value.Value.Eq(UnboundMarker))
                        throw new LispSymbolUnboundException(this);
                    return data.LocalValue.Value.Value;
                }
                if (!data.GlobalValue.HasValue)
                    throw new LispSymbolUnboundException(this);
                return data.GlobalValue.Value;
            }
            set
            {
                CheckSymbol();
                var data = GetData<ISymbolData>();
                if (data.LocalValue != null && data.LocalValue.Value.HasValue)
                    data.LocalValue.Value = value;
                else
                {
                    CheckNotConstantSymbol();
                    lock (data)
                    {
                        data.GlobalValue = value;
                    }
                }
            }
        }

        public IDisposable SymbolBind(LispObject value)
        {
            CheckSymbol();
            var data = GetData<ISymbolData>();
            if (data.LocalValue == null)
                lock (data)
                {
                    data.LocalValue = new ThreadLocal<LispObject?>();
                }
            var prev = data.LocalValue.Value;
            data.LocalValue.Value = value;
            return new DelegateDisposable(() => data.LocalValue.Value = prev);
        }

        public void SymbolMakunbound()
        {
            CheckSymbol();
            CheckNotConstantSymbol();
            var data = GetData<ISymbolData>();
            if (data.LocalValue != null && data.LocalValue.Value.HasValue)
                data.LocalValue.Value = UnboundMarker;
            else
                lock (data)
                {
                    data.GlobalValue = null;
                }
        }

        public bool IsSymbolBound
        {
            get
            {
                CheckSymbol();
                var data = GetData<ISymbolData>();
                if (data.LocalValue != null && data.LocalValue.Value.HasValue)
                    return !data.LocalValue.Value.Value.Eq(UnboundMarker);
                return data.GlobalValue.HasValue;
            }
        }

        public bool IsSymbolConstant
        {
            get
            {
                CheckSymbol();
                LispVar var;
                return Vars.TryGetValue(this, out var) && var.Kind == LispVarKind.Constant;
            }
        }

        public LispVar SymbolDefvar(LispVarKind kind)
        {
            CheckSymbol();
            var name = this;
            LispVar var;
            if (Vars.TryGetValue(name, out var))
            {
                if (var.Kind != kind)
                    throw new LispVariableRedeclarationException(name);
                return var;
            }
            return Vars[name] = new LispVar(name, kind);
        }

        public LispVar SymbolVar
        {
            get
            {
                CheckSymbol();
                LispVar var;
                Vars.TryGetValue(this, out var);
                return var;
            }
        }

        public bool SymbolUndefVar()
        {
            CheckSymbol();
            if (IsT || IsNil) throw new LispConstantModificationException(this);
            var name = this;
            LispVar var;
            return Vars.TryRemove(name, out var);
        }

        #endregion

        #region List operations

        public static LispObject CreateListFromEnumerable(IEnumerable<LispObject> elements)
        {
            elements = elements ?? Enumerable.Empty<LispObject>();
            var e = elements.GetEnumerator();
            if (!e.MoveNext()) return Nil;
            var head = CreateCons(e.Current, Nil);
            var tail = head;
            while (e.MoveNext())
            {
                var tmp = CreateCons(e.Current, Nil);
                tail.Cdr = tmp;
                tail = tmp;
            }
            return head;
        }

        public static LispObject CreateList(params LispObject[] args)
        {
            return CreateListFromEnumerable(args);
        }

        public int ListLength
        {
            get
            {
                CheckList();
                if (IsNil) return 0;
                return 1 + Cdr.ListLength;
            }
        }

        public bool IsListEnd
        {
            get
            {
                CheckList();
                return IsNil;
            }
        }

        public bool IsProperList
        {
            get
            {
                CheckList();
                var l = this;
                while (l.IsCons)
                {
                    l = l.Cdr;
                }
                return l.IsList;
            }
        }

        public IEnumerable<LispObject> AsListEnumerable()
        {
            CheckList();
            var current = this;
            while (true)
            {
                if (current.IsNil) yield break;
                var elt = current.Car;
                current = current.Cdr;
                yield return elt;
            }
        }

        public void FillList(LispObject o)
        {
            CheckList();
            if (IsNil) return;
            var cons = this;
            do
            {
                cons.Car = o;
                cons = cons.Cdr;
            } while (!cons.IsListEnd);
        }

        public LispObject ListRef(int index)
        {
            CheckList();
            if (index < 0 || IsNil)
                throw new LispRangeContraintException(CreateFixnum(index), CreateFixnum(0), CreateFixnum(ListLength));
            var cons = this;
            var i = 0;
            do
            {
                if (i == index) return cons.Car;
                ++i;
                cons = cons.Cdr;
            } while (!cons.IsListEnd);
            throw new LispRangeContraintException(CreateFixnum(index), CreateFixnum(0), CreateFixnum(i));
        }

        public void ListSet(int index, LispObject value)
        {
            CheckList();
            if (index < 0 || IsNil)
                throw new LispRangeContraintException(CreateFixnum(index), CreateFixnum(0), CreateFixnum(ListLength));
            var cons = this;
            var i = 0;
            do
            {
                if (i == index)
                {
                    cons.Car = value;
                }
                ++i;
                cons = cons.Cdr;
            } while (!cons.IsListEnd);
            throw new LispRangeContraintException(CreateFixnum(index), CreateFixnum(0), CreateFixnum(i));
        }

        public LispObject ListNth(int index)
        {
            if (index < 0) throw new LispRangeContraintException(CreateFixnum(index), CreateFixnum(0), Nil);
            return AsListEnumerable().Skip(index).FirstOrDefault();
        }

        public LispObject ListNthCdr(int index)
        {
            CheckList();
            if (index < 0) throw new LispRangeContraintException(CreateFixnum(index), CreateFixnum(0), Nil);
            var l = this;
            while (!l.IsNil && index > 0)
            {
                l = l.Cdr;
                --index;
            }
            return l;
        }

        #endregion

        #region Vector operations

        public IEnumerable<LispObject> AsVectorEnumerable()
        {
            CheckVector();
            return GetData<LispObject[]>().AsEnumerable();
        }

        public void FillVector(LispObject o)
        {
            CheckVector();
            var data = GetData<LispObject[]>();
            for (var i = 0; i < data.Length; ++i)
                data[i] = o;
        }

        #endregion

        #region String operations

        public IEnumerable<LispObject> AsStringEnumerable()
        {
            CheckString();
            var data = GetData<StringData>().Value;
            return data.Select(CreateChar);
        }

        public void FillString(int c)
        {
            CheckString();
            var data = GetData<StringData>().Value;
            for (var i = 0; i < data.Length; i++)
                data[i] = c;
        }

        public void FillString(LispObject c)
        {
            CheckString();
            FillString(c.CharValue);
        }

        public bool StringEqual(LispObject s)
        {
            CheckString();
            s.CheckString();
            return GetData<StringData>().Equals(s.GetData<StringData>());
        }

        #endregion

        #region Sequence operations

        public IEnumerable<LispObject> AsEnumerable()
        {
            if (IsVector)
                return AsVectorEnumerable();
            if (IsString)
                return AsStringEnumerable();
            if (IsList)
                return AsListEnumerable();
            throw new LispObjectNotSequenceException(this);
        }

        public LispObject CoerceToString()
        {
            if (IsString)
                return this;
            if (IsChar)
                return CreateString(new[] { CharValue });
            return IsSymbol ? SymbolName : CreateString(AsEnumerable().Select(x => x.CharValue));
        }

        public LispObject CoerceToList()
        {
            return CreateListFromEnumerable(AsEnumerable());
        }

        public LispObject CoerceToVector()
        {
            return CreateVector(AsEnumerable());
        }

        public LispObject Append(LispObject sequence)
        {
            return GetSequenceFromEnumerable(AsEnumerable().Concat(sequence.AsEnumerable()));
        }

        public LispObject AppendToList(LispObject sequence)
        {
            return CreateListFromEnumerable(AsEnumerable().Concat(sequence.AsEnumerable()));
        }

        public LispObject AppendToVector(LispObject sequence)
        {
            return CreateVector(AsEnumerable().Concat(sequence.AsEnumerable()));
        }

        public LispObject AppendToString(LispObject sequence)
        {
            return CreateString(AsEnumerable().Concat(sequence.AsEnumerable()).Select(x => x.CharValue));
        }

        public LispObject ReverseToList()
        {
            return CreateListFromEnumerable(AsEnumerable().Reverse());
        }

        public LispObject ReverseToVector()
        {
            return CreateVector(AsEnumerable().Reverse());
        }

        public LispObject ReverseToString()
        {
            return CreateString(AsEnumerable().Reverse().Select(x => x.CharValue));
        }

        public LispObject Reverse()
        {
            return GetSequenceFromEnumerable(AsEnumerable().Reverse());
        }

        public void Fill(LispObject o)
        {
            if (IsList)
                FillList(o);
            if (IsVector)
                FillVector(o);
            if (IsString)
                FillString(o);
            throw new LispObjectNotSequenceException(this);
        }

        public LispObject SequenceCopy
        {
            get { return GetSequenceFromEnumerable(AsEnumerable()); }
        }

        public int SequenceLength
        {
            get
            {
                if (IsList) return ListLength;
                if (IsString) return StringLength;
                if (IsVector) return VectorLength;
                throw new LispObjectNotSequenceException(this);
            }
        }

        private LispObject GetSequenceFromEnumerable(IEnumerable<LispObject> seq)
        {
            if (IsList) return CreateListFromEnumerable(seq);
            if (IsVector) return CreateVector(seq);
            if (IsString) return CreateString(seq.Select(x => x.CharValue));
            throw new LispObjectNotSequenceException(this);
        }

        #endregion

        #region Equality predicates

        public bool Eq(LispObject o)
        {
            return ReferenceEquals(_data, o._data);
        }

        public bool Eql(LispObject o)
        {
            if (Eq(o)) return true;
            if (IsFixnum)
                return o.IsFixnum && FixnumValue == o.FixnumValue;
            if (IsBignum)
                return o.IsBignum && IntegerValue == o.IntegerValue;
            if (IsFloat)
                return o.IsFloat && Math.Abs(FloatValue - o.FloatValue) < float.Epsilon;
            if (IsDouble)
                return o.IsDouble && Math.Abs(DoubleValue - o.DoubleValue) < double.Epsilon;
            if (IsChar)
                return o.IsChar && CharValue == o.CharValue;
            if (IsRatioFixnum && o.IsRatioFixnum)
            {
                var data = GetData<RatioFixnumData>();
                var oData = GetData<RatioFixnumData>();
                return data.Numerator == oData.Numerator && data.Denominator == oData.Denominator;
            }
            if (IsRatioInteger && o.IsRatioInteger)
            {
                var data = GetData<RatioIntegerData>();
                var oData = GetData<RatioIntegerData>();
                return data.Numerator == oData.Numerator && data.Denominator == oData.Denominator;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return GetData<object>().GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LispObject)) return false;
            return Equals((LispObject)obj);
        }

        public bool Equals(LispObject obj)
        {
            if (Eql(obj)) return true;
            if (obj.Type != Type) return false;
            var data = GetData<object>();
            var objData = obj.GetData<object>();
            return data.Equals(objData);
        }

        #endregion

        #region Math

        #region Basic arithmetics

        public LispObject Add(LispObject x)
        {
            if (IsInteger)
            {
                var v = IsFixnum ? GetData<int>() : GetData<BigInteger>();
                if (x.IsInteger) return Add(v, x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>());
                if (x.IsFloat) return Add(v, x.GetData<float>());
                if (x.IsDouble) return Add(v, x.GetData<double>());
                if (x.IsRatioFixnum) return Add(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Add(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsFloat)
            {
                var v = GetData<float>();
                if (x.IsInteger) return Add(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Add(v, x.GetData<float>());
                if (x.IsDouble) return Add(v, x.GetData<double>());
                if (x.IsRatioFixnum) return Add(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Add(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsDouble)
            {
                var v = GetData<double>();
                if (x.IsInteger) return Add(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Add(x.GetData<float>(), v);
                if (x.IsDouble) return Add(v, x.GetData<double>());
                if (x.IsRatioFixnum) return Add(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Add(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsRatioFixnum)
            {
                var v = GetData<RatioFixnumData>();
                if (x.IsInteger) return Add(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Add(x.GetData<float>(), v);
                if (x.IsDouble) return Add(x.GetData<double>(), v);
                if (x.IsRatioFixnum) return Add(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Add(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsRatioInteger)
            {
                var v = GetData<RatioIntegerData>();
                if (x.IsInteger) return Add(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Add(x.GetData<float>(), v);
                if (x.IsDouble) return Add(x.GetData<double>(), v);
                if (x.IsRatioFixnum) return Add(x.GetData<RatioFixnumData>(), v);
                if (x.IsRatioInteger) return Add(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            throw new LispObjectNotNumberException(this);
        }

        private static LispObject Add(BigInteger x, BigInteger y)
        {
            return CreateInteger(x + y);
        }

        private static LispObject Add(BigInteger x, float y)
        {
            return CreateFloat((float)x + y);
        }

        private static LispObject Add(BigInteger x, double y)
        {
            return CreateDouble((double)x + y);
        }

        private static LispObject Add(BigInteger x, RatioFixnumData y)
        {
            return CreateRatio(y.Numerator + x * y.Denominator, y.Denominator);
        }

        private static LispObject Add(BigInteger x, RatioIntegerData y)
        {
            return CreateRatio(y.Numerator + x * y.Denominator, y.Denominator);
        }

        private static LispObject Add(float x, float y)
        {
            return CreateFloat(x + y);
        }

        private static LispObject Add(float x, double y)
        {
            return CreateDouble(x + y);
        }

        private static LispObject Add(float x, RatioFixnumData y)
        {
            return CreateFloat((y.Numerator + x * y.Denominator) / y.Denominator);
        }

        private static LispObject Add(float x, RatioIntegerData y)
        {
            return CreateFloat(((float)y.Numerator + x * (float)y.Denominator) / (float)y.Denominator);
        }

        private static LispObject Add(double x, double y)
        {
            return CreateDouble(x + y);
        }

        private static LispObject Add(double x, RatioFixnumData y)
        {
            return CreateDouble((y.Numerator + x * y.Denominator) / y.Denominator);
        }

        private static LispObject Add(double x, RatioIntegerData y)
        {
            return CreateDouble(((double)y.Numerator + x * (double)y.Denominator) / (double)y.Denominator);
        }

        private static LispObject Add(RatioFixnumData x, RatioFixnumData y)
        {
            var d = x.Denominator * (BigInteger)y.Denominator;
            return CreateRatio(x.Numerator * y.Denominator + y.Numerator * x.Denominator, d);
        }

        private static LispObject Add(RatioFixnumData x, RatioIntegerData y)
        {
            var d = x.Denominator * y.Denominator;
            return CreateRatio(x.Numerator * y.Denominator + y.Numerator * x.Denominator, d);
        }

        private static LispObject Add(RatioIntegerData x, RatioIntegerData y)
        {
            var d = x.Denominator * y.Denominator;
            return CreateRatio(x.Numerator * y.Denominator + y.Numerator * x.Denominator, d);
        }

        public LispObject Neg
        {
            get
            {
                if (IsFixnum) return CreateInteger(-GetData<int>());
                if (IsInteger) return CreateInteger(-GetData<BigInteger>());
                if (IsFloat) return CreateFloat(-GetData<float>());
                if (IsDouble) return CreateDouble(-GetData<double>());
                if (IsRatioFixnum)
                {
                    var r = GetData<RatioFixnumData>();
                    return CreateRatio(-(BigInteger)r.Numerator, r.Denominator);
                }
                if (IsRatioInteger)
                {
                    var r = GetData<RatioIntegerData>();
                    return CreateRatio(-r.Numerator, r.Denominator);
                }
                throw new LispObjectNotNumberException(this);
            }
        }

        public LispObject Sub(LispObject x)
        {
            return Add(x.Neg);
        }

        public LispObject Mul(LispObject x)
        {
            if (IsInteger)
            {
                var v = IsFixnum ? GetData<int>() : GetData<BigInteger>();
                if (x.IsInteger) return Mul(v, x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>());
                if (x.IsFloat) return Mul(v, x.GetData<float>());
                if (x.IsDouble) return Mul(v, x.GetData<double>());
                if (x.IsRatioFixnum) return Mul(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Mul(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsFloat)
            {
                var v = GetData<float>();
                if (x.IsInteger) return Mul(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Mul(v, x.GetData<float>());
                if (x.IsDouble) return Mul(v, x.GetData<double>());
                if (x.IsRatioFixnum) return Mul(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Mul(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsDouble)
            {
                var v = GetData<double>();
                if (x.IsInteger) return Mul(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Mul(x.GetData<float>(), v);
                if (x.IsDouble) return Mul(v, x.GetData<double>());
                if (x.IsRatioFixnum) return Mul(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Mul(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsRatioFixnum)
            {
                var v = GetData<RatioFixnumData>();
                if (x.IsInteger) return Mul(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Mul(x.GetData<float>(), v);
                if (x.IsDouble) return Mul(x.GetData<double>(), v);
                if (x.IsRatioFixnum) return Mul(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return Mul(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            if (IsRatioInteger)
            {
                var v = GetData<RatioIntegerData>();
                if (x.IsInteger) return Mul(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v);
                if (x.IsFloat) return Mul(x.GetData<float>(), v);
                if (x.IsDouble) return Mul(x.GetData<double>(), v);
                if (x.IsRatioFixnum) return Mul(x.GetData<RatioFixnumData>(), v);
                if (x.IsRatioInteger) return Mul(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotNumberException(x);
            }
            throw new LispObjectNotNumberException(this);
        }

        private static LispObject Mul(BigInteger x, BigInteger y)
        {
            return CreateInteger(x * y);
        }

        private static LispObject Mul(BigInteger x, float y)
        {
            return CreateFloat((float)x * y);
        }

        private static LispObject Mul(BigInteger x, double y)
        {
            return CreateDouble((double)x * y);
        }

        private static LispObject Mul(BigInteger x, RatioFixnumData y)
        {
            return CreateRatio(x * y.Numerator, y.Denominator);
        }

        private static LispObject Mul(BigInteger x, RatioIntegerData y)
        {
            return CreateRatio(x * y.Numerator, y.Denominator);
        }

        private static LispObject Mul(float x, float y)
        {
            return CreateFloat(x * y);
        }

        private static LispObject Mul(float x, double y)
        {
            return CreateDouble(x * y);
        }

        private static LispObject Mul(float x, RatioFixnumData y)
        {
            return CreateFloat(y.Numerator / (float)y.Denominator * x);
        }

        private static LispObject Mul(float x, RatioIntegerData y)
        {
            return CreateFloat((float)y.Numerator / (float)y.Denominator * x);
        }

        private static LispObject Mul(double x, double y)
        {
            return CreateDouble(x * y);
        }

        private static LispObject Mul(double x, RatioFixnumData y)
        {
            return CreateDouble(y.Numerator / (double)y.Denominator * x);
        }

        private static LispObject Mul(double x, RatioIntegerData y)
        {
            return CreateDouble((double)y.Numerator / (double)y.Denominator * x);
        }

        private static LispObject Mul(RatioFixnumData x, RatioFixnumData y)
        {
            return CreateRatio(x.Numerator * (BigInteger)y.Numerator, x.Denominator * (BigInteger)x.Denominator);
        }

        private static LispObject Mul(RatioFixnumData x, RatioIntegerData y)
        {
            return CreateRatio(x.Numerator * y.Numerator, x.Denominator * y.Denominator);
        }

        private static LispObject Mul(RatioIntegerData x, RatioIntegerData y)
        {
            return CreateRatio(x.Numerator * y.Numerator, x.Denominator * y.Denominator);
        }

        public LispObject OneDivBy
        {
            get
            {
                if (IsFixnum) return CreateRatio(1, GetData<int>());
                if (IsInteger) return CreateRatio(1, GetData<BigInteger>());
                if (IsFloat) return CreateFloat(1 / GetData<float>());
                if (IsDouble) return CreateDouble(1 / GetData<double>());
                if (IsRatioFixnum)
                {
                    var r = GetData<RatioFixnumData>();
                    return CreateRatio(r.Denominator, r.Numerator);
                }
                if (IsRatioInteger)
                {
                    var r = GetData<RatioIntegerData>();
                    return CreateRatio(r.Denominator, r.Numerator);
                }
                throw new LispObjectNotNumberException(this);
            }
        }

        public LispObject Div(LispObject x)
        {
            return Mul(x.OneDivBy);
        }

        public int NumberCompare(LispObject x)
        {
            if (IsInteger)
            {
                var v = IsFixnum ? GetData<int>() : GetData<BigInteger>();
                if (x.IsInteger) return NumberCompare(v, x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>());
                if (x.IsFloat) return NumberCompare(v, x.GetData<float>());
                if (x.IsDouble) return NumberCompare(v, x.GetData<double>());
                if (x.IsRatioFixnum) return NumberCompare(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return NumberCompare(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotRealException(x);
            }
            if (IsFloat)
            {
                var v = GetData<float>();
                if (x.IsInteger) return NumberCompare(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v) * -1;
                if (x.IsFloat) return NumberCompare(v, x.GetData<float>());
                if (x.IsDouble) return NumberCompare(v, x.GetData<double>());
                if (x.IsRatioFixnum) return NumberCompare(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return NumberCompare(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotRealException(x);
            }
            if (IsDouble)
            {
                var v = GetData<double>();
                if (x.IsInteger) return NumberCompare(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v) * -1;
                if (x.IsFloat) return NumberCompare(x.GetData<float>(), v) * -1;
                if (x.IsDouble) return NumberCompare(v, x.GetData<double>());
                if (x.IsRatioFixnum) return NumberCompare(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return NumberCompare(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotRealException(x);
            }
            if (IsRatioFixnum)
            {
                var v = GetData<RatioFixnumData>();
                if (x.IsInteger) return NumberCompare(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v) * -1;
                if (x.IsFloat) return NumberCompare(x.GetData<float>(), v) * -1;
                if (x.IsDouble) return NumberCompare(x.GetData<double>(), v) * -1;
                if (x.IsRatioFixnum) return NumberCompare(v, x.GetData<RatioFixnumData>());
                if (x.IsRatioInteger) return NumberCompare(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotRealException(x);
            }
            if (IsRatioInteger)
            {
                var v = GetData<RatioIntegerData>();
                if (x.IsInteger) return NumberCompare(x.IsFixnum ? x.GetData<int>() : x.GetData<BigInteger>(), v) * -1;
                if (x.IsFloat) return NumberCompare(x.GetData<float>(), v) * -1;
                if (x.IsDouble) return NumberCompare(x.GetData<double>(), v) * -1;
                if (x.IsRatioFixnum) return NumberCompare(x.GetData<RatioFixnumData>(), v) * -1;
                if (x.IsRatioInteger) return NumberCompare(v, x.GetData<RatioIntegerData>());
                throw new LispObjectNotRealException(x);
            }
            throw new LispObjectNotRealException(this);
        }

        private static int NumberCompare(BigInteger x, BigInteger y)
        {
            return x.CompareTo(y);
        }

        private static int NumberCompare(BigInteger x, float y)
        {
            return ((float)x).CompareTo(y);
        }

        private static int NumberCompare(BigInteger x, double y)
        {
            return ((double)x).CompareTo(y);
        }

        private static int NumberCompare(BigInteger x, RatioFixnumData y)
        {
            return (x * y.Denominator).CompareTo(y.Numerator);
        }

        private static int NumberCompare(BigInteger x, RatioIntegerData y)
        {
            return (x * y.Denominator).CompareTo(y.Numerator);
        }

        private static int NumberCompare(float x, float y)
        {
            return x.CompareTo(y);
        }

        private static int NumberCompare(float x, double y)
        {
            return ((double)x).CompareTo(y);
        }

        private static int NumberCompare(float x, RatioFixnumData y)
        {
            return x.CompareTo(y.Numerator / (float)y.Denominator);
        }

        private static int NumberCompare(float x, RatioIntegerData y)
        {
            return x.CompareTo((float)y.Numerator / (float)y.Denominator);
        }

        private static int NumberCompare(double x, double y)
        {
            return x.CompareTo(y);
        }

        private static int NumberCompare(double x, RatioFixnumData y)
        {
            return x.CompareTo(y.Numerator / (double)y.Denominator);
        }

        private static int NumberCompare(double x, RatioIntegerData y)
        {
            return x.CompareTo((double)y.Numerator / (double)y.Denominator);
        }

        private static int NumberCompare(RatioFixnumData x, RatioFixnumData y)
        {
            return (x.Numerator * (BigInteger)y.Denominator).CompareTo(y.Numerator * (BigInteger)x.Denominator);
        }

        private static int NumberCompare(RatioFixnumData x, RatioIntegerData y)
        {
            return (x.Numerator * y.Denominator).CompareTo(y.Numerator * x.Denominator);
        }

        private static int NumberCompare(RatioIntegerData x, RatioIntegerData y)
        {
            return (x.Numerator * y.Denominator).CompareTo(y.Numerator * x.Denominator);
        }

        public bool NumberEq(LispObject x)
        {
            return NumberCompare(x) == 0;
        }

        public bool NumberGt(LispObject x)
        {
            return NumberCompare(x) > 0;
        }

        public bool NumberGte(LispObject x)
        {
            return NumberCompare(x) >= 0;
        }

        public bool NumberLt(LispObject x)
        {
            return NumberCompare(x) < 0;
        }

        public bool NumberLte(LispObject x)
        {
            return NumberCompare(x) <= 0;
        }

        public LispObject Abs
        {
            get
            {
                if (IsFixnum) return CreateInteger(Math.Abs(GetData<int>()));
                if (IsInteger) return CreateInteger(BigInteger.Abs(GetData<BigInteger>()));
                if (IsFloat) return CreateFloat(Math.Abs(GetData<float>()));
                if (IsDouble) return CreateDouble(Math.Abs(GetData<double>()));
                if (IsRatioFixnum)
                {
                    var d = GetData<RatioFixnumData>();
                    return CreateRatio(BigInteger.Abs(d.Numerator), d.Denominator);
                }
                if (IsRatioFixnum)
                {
                    var d = GetData<RatioIntegerData>();
                    return CreateRatio(BigInteger.Abs(d.Numerator), d.Denominator);
                }
                throw new LispObjectNotNumberException(this);
            }
        }

        public bool IsPositive
        {
            get
            {
                if (IsFixnum) return GetData<int>() > 0;
                if (IsInteger) return GetData<BigInteger>() > 0;
                if (IsFloat) return GetData<float>() > 0.0f;
                if (IsDouble) return GetData<double>() > 0.0;
                if (IsRatioFixnum) return GetData<RatioFixnumData>().Numerator > 0;
                if (IsRatioInteger) return GetData<RatioIntegerData>().Numerator > 0;
                throw new LispObjectNotNumberException(this);
            }
        }

        public bool IsNegative
        {
            get
            {
                if (IsFixnum) return GetData<int>() < 0;
                if (IsInteger) return GetData<BigInteger>() < 0;
                if (IsFloat) return GetData<float>() < 0.0f;
                if (IsDouble) return GetData<double>() < 0.0;
                if (IsRatioFixnum) return GetData<RatioFixnumData>().Numerator < 0;
                if (IsRatioInteger) return GetData<RatioIntegerData>().Numerator < 0;
                throw new LispObjectNotNumberException(this);
            }
        }

        public bool IsZero
        {
            get
            {
                if (IsFixnum) return GetData<int>() == 0;
                if (IsInteger) return GetData<BigInteger>() == 0;
                if (IsFloat) return GetData<float>() - 0.0f < float.Epsilon;
                if (IsDouble) return GetData<double>() - 0.0 < double.Epsilon;
                if (IsRatioFixnum || IsRatioInteger)
                    return false;
                throw new LispObjectNotNumberException(this);
            }
        }

        public int Sig
        {
            get { return NumberCompare(CreateFixnum(0)); }
        }

        #endregion

        #endregion
    }
}
