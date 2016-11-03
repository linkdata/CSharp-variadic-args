# CSharp-variadic-args
Performant ways of emulating C's VARARGS using P/Invoke or unsafe C#

Provides two sample implementations for passing varidic arguments from C# to native (C).

Both implementations will pin pointer types (`byte[]` and `string`) for the duration of the native call. The `unsafe` implementation runs faster only because it can use `stackalloc` and avoiding to copy the `VarArg` structure that the safe version uses.

```
2915 calls/millisecond for SAFE "OnlyValueTypes"
4792 calls/millisecond for UNSAFE "OnlyValueTypes"
2031 calls/millisecond for SAFE "OnlyValueTypesAndString"
2808 calls/millisecond for UNSAFE "OnlyValueTypesAndString"
1483 calls/millisecond for SAFE "AllTypes"
1805 calls/millisecond for UNSAFE "AllTypes"
```
