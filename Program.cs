using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CSharpVarArgsNative
{
    public static class NativeAPI
    {
        public const long STAR_TYPE_STRING = 0x01;
        public const long STAR_TYPE_BINARY = 0x02;
        public const long STAR_TYPE_LONG = 0x03;
        public const long STAR_TYPE_ULONG = 0x04;
        public const long STAR_TYPE_DECIMAL = 0x05; // Stored as a ULONG using X6Decimal, so not tested
        public const long STAR_TYPE_FLOAT = 0x06;
        public const long STAR_TYPE_DOUBLE = 0x07;
        public const long STAR_TYPE_REFERENCE = 0x08;

        public struct Ref
        {
            public Ref(ulong dbId, ulong dbRef)
            {
                DbId = dbId;
                DbRef = dbRef;
            }
            public ulong DbId;
            public ulong DbRef;
        };

        public struct VarArg
        {
            public long TypeCode;
            public long LongValue;
            public ulong ULongValue;
            public double DoubleValue;
            public IntPtr PointerValue;
        }

        private delegate VarArg ArgMaker(object obj, ref GCHandle h);

        private static Dictionary<Type, ArgMaker> _argMakers = new Dictionary<Type, ArgMaker>()
        {
            { typeof(string), (object o, ref GCHandle h) =>
                {
                    h = GCHandle.Alloc(o, GCHandleType.Pinned);
                    return new VarArg() { TypeCode = STAR_TYPE_STRING, PointerValue = h.AddrOfPinnedObject() };
                }
            },
            { typeof(byte[]), (object o, ref GCHandle h) =>
                {
                    h = GCHandle.Alloc(o, GCHandleType.Pinned);
                    return new VarArg() { TypeCode = STAR_TYPE_BINARY, LongValue = ((byte[])o).Length, PointerValue = h.AddrOfPinnedObject() };
                }
            },
            { typeof(long), (object o, ref GCHandle h) => { return new VarArg() { TypeCode = STAR_TYPE_LONG, LongValue = (long)o }; } },
            { typeof(ulong), (object o, ref GCHandle h) => { return new VarArg() { TypeCode = STAR_TYPE_ULONG, ULongValue = (ulong)o }; } },
            { typeof(float), (object o, ref GCHandle h) => { return new VarArg() { TypeCode = STAR_TYPE_FLOAT, DoubleValue = (float)o }; } },
            { typeof(double), (object o, ref GCHandle h) => { return new VarArg() { TypeCode = STAR_TYPE_DOUBLE, DoubleValue = (double)o }; } },
            { typeof(Ref), (object o, ref GCHandle h) => { Ref r = (Ref)o;  return new VarArg() { TypeCode = STAR_TYPE_REFERENCE, LongValue = (long)r.DbId, ULongValue = r.DbRef }; } },
        };

        public static void Variadic(StringBuilder sb, params object[] args)
        {
            VarArg[] varargs = new VarArg[args.Length];
            GCHandle[] handles = new GCHandle[args.Length];
            ArgMaker maker;
            for (int i = 0; i < args.Length; i++)
            {
                object obj = args[i];
                if (!_argMakers.TryGetValue(obj.GetType(), out maker))
                    throw new Exception("Type not supported: " + obj.GetType().FullName);
                varargs[i] = maker(obj, ref handles[i]);
            }
            GCHandle handle = GCHandle.Alloc(varargs, GCHandleType.Pinned);
            CSharpVarArgsNative(sb, (sb == null ? 0U : (uint)sb.Capacity), varargs.Length, handle.AddrOfPinnedObject());
            foreach (var h in handles)
                if (h.IsAllocated)
                    h.Free();
            handle.Free();
            return;
        }

        private unsafe delegate void UnsafeArgBuilder(object obj, ref GCHandle h, long* varargs);

        private static unsafe UnsafeArgBuilder _buildString = (object o, ref GCHandle h, long* varargs) =>
        {
            h = GCHandle.Alloc(o, GCHandleType.Pinned);
            *varargs++ = STAR_TYPE_STRING; // TypeCode
            *varargs++ = 0; // LongValue
            *varargs++ = 0; // ULongValue
            *varargs++ = 0; // double
            *varargs++ = h.AddrOfPinnedObject().ToInt64(); // Pointer
        };

        private static unsafe UnsafeArgBuilder _buildBinary = (object o, ref GCHandle h, long* varargs) =>
        {
            h = GCHandle.Alloc(o, GCHandleType.Pinned);
            *varargs++ = STAR_TYPE_BINARY; // TypeCode
            *varargs++ = ((byte[])o).Length; // LongValue
            *varargs++ = 0; // ULongValue
            *varargs++ = 0; // double
            *varargs++ = h.AddrOfPinnedObject().ToInt64(); // Pointer
        };

        private static unsafe UnsafeArgBuilder _buildLong = (object o, ref GCHandle h, long* varargs) =>
        {
            *varargs++ = STAR_TYPE_LONG; // TypeCode
            *varargs++ = (long)o; // LongValue
            *varargs++ = 0; // ULongValue
            *varargs++ = 0; // double
            *varargs++ = 0; // Pointer
        };

        private static unsafe UnsafeArgBuilder _buildULong = (object o, ref GCHandle h, long* varargs) =>
        {
            *varargs++ = STAR_TYPE_ULONG; // TypeCode
            *varargs++ = 0; // LongValue
            *(ulong*)varargs++ = (ulong)o; // ULongValue
            *varargs++ = 0; // double
            *varargs++ = 0; // Pointer
        };

        private static unsafe UnsafeArgBuilder _buildFloat = (object o, ref GCHandle h, long* varargs) =>
        {
            *varargs++ = STAR_TYPE_FLOAT; // TypeCode
            *varargs++ = 0; // LongValue
            *varargs++ = 0; // ULongValue
            *(double*)varargs++ = (float)o; // double
            *varargs++ = 0; // Pointer
        };

        private static unsafe UnsafeArgBuilder _buildDouble = (object o, ref GCHandle h, long* varargs) =>
        {
            *varargs++ = STAR_TYPE_DOUBLE; // TypeCode
            *varargs++ = 0; // LongValue
            *varargs++ = 0; // ULongValue
            *(double*)varargs++ = (double)o; // double
            *varargs++ = 0; // Pointer
        };

        private static unsafe UnsafeArgBuilder _buildRef = (object o, ref GCHandle h, long* varargs) =>
        {
            Ref r = (Ref)o;
            *varargs++ = STAR_TYPE_REFERENCE; // TypeCode
            *(ulong*)varargs++ = r.DbId; // LongValue
            *(ulong*)varargs++ = r.DbRef; // ULongValue
            *varargs++ = 0; // double
            *varargs++ = 0; // Pointer
        };

        private static Dictionary<Type, UnsafeArgBuilder> _argBuilders = new Dictionary<Type, UnsafeArgBuilder>()
        {
            { typeof(string), _buildString },
            { typeof(byte[]), _buildBinary },
            { typeof(long), _buildLong },
            { typeof(ulong), _buildULong },
            { typeof(float), _buildFloat },
            { typeof(double), _buildDouble },
            { typeof(Ref), _buildRef },
        };

        public static unsafe void UnsafeVariadic(StringBuilder sb, params object[] args)
        {
            long* varargs = stackalloc long[args.Length * 5];
            GCHandle[] handles = new GCHandle[args.Length];
            UnsafeArgBuilder builder;
            long* ptr = varargs;
            for (int i = 0; i < args.Length; i++)
            {
                object obj = args[i];
                if (!_argBuilders.TryGetValue(obj.GetType(), out builder))
                    throw new Exception("Type not supported: " + obj.GetType().FullName);
                builder(obj, ref handles[i], ptr);
                ptr += 5;
            }
            CSharpVarArgsNative(sb, (sb == null ? 0U : (uint)sb.Capacity), args.Length, new IntPtr(varargs));
            foreach (var h in handles)
                if (h.IsAllocated)
                    h.Free();
            return;
        }

        [DllImport("NativeAPI.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
        private static extern uint CSharpVarArgsNative(StringBuilder buf, uint maxbuf, long argc, IntPtr argv);
    }

    class Program
    {
        static void BenchmarkSafe(string text, params object[] args)
        {
            const int numcalls = 3000000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < numcalls; i++)
                NativeAPI.Variadic(null, args);
            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine("{0} calls/millisecond for SAFE \"{1}\"", numcalls / elapsed, text);
        }

        static void BenchmarkUnsafe(string text, params object[] args)
        {
            const int numcalls = 3000000;
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < numcalls; i++)
                NativeAPI.UnsafeVariadic(null, args);
            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine("{0} calls/millisecond for UNSAFE \"{1}\"", numcalls / elapsed, text);
        }

        static void Benchmark(string text, params object[] args)
        {
            BenchmarkSafe(text, args);
            BenchmarkUnsafe(text, args);
        }

        static void Correctness(string expect, params object[] args)
        {
            StringBuilder sb;
            string actual;

            sb = new StringBuilder(512);
            NativeAPI.Variadic(sb, args);
            actual = sb.ToString();
            if (expect != actual)
            {
                Console.WriteLine("Error: SAFE Expect \"{0}\"", expect);
                Console.WriteLine("       SAFE Actual \"{0}\"", actual);
            }

            sb = new StringBuilder(512);
            NativeAPI.UnsafeVariadic(sb, args);
            actual = sb.ToString();
            if (expect != actual)
            {
                Console.WriteLine("Error: UNSAFE Expect \"{0}\"", expect);
                Console.WriteLine("       UNSAFE Actual \"{0}\"", actual);
            }
        }

        static void Main(string[] args)
        {
            Correctness("[LONG=1] [STRING='2'] [DOUBLE=3.125] [FLOAT=1.0001] [ULONG=4] [BINARY: 1 2 3 4 5] [REFERENCE:777@999] ",
                1L, "2", 3.125, 1.0001F, 4UL, new byte[5] { 1, 2, 3, 4, 5 }, new NativeAPI.Ref(777, 999));
            Benchmark("OnlyValueTypes", 1L, 3.125, 4UL, new NativeAPI.Ref(777, 999));
            Benchmark("OnlyValueTypesAndString", 1L, "2", 3.125, 4UL, new NativeAPI.Ref(777, 999));
            Benchmark("AllTypes", 1L, "2", 3.125, 4UL, new byte[5] { 1, 2, 3, 4, 5 }, new NativeAPI.Ref(777, 999));
        }
    }
}
