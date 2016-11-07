# CSharp-variadic-args
Performant ways of emulating C's VARARGS using P/Invoke or unsafe C#

Provides two sample implementations for passing varidic arguments from C# to native (C).

Both implementations will pin pointer types (`byte[]` and `string`) for the duration of the native call. The `unsafe` implementation runs faster only because it can use `stackalloc` and avoiding to copy the `VarArg` structure that the safe version uses.

```
3205 calls/millisecond for SAFE "OnlyValueTypes"
4918 calls/millisecond for UNSAFE "OnlyValueTypes"
2209 calls/millisecond for SAFE "OnlyValueTypesAndString"
2952 calls/millisecond for UNSAFE "OnlyValueTypesAndString"
1617 calls/millisecond for SAFE "AllTypes"
1944 calls/millisecond for UNSAFE "AllTypes"
```

An interesting note here is that in some cases, pinning strings and buffers isn't the most performant way of passing these values. In the case of a slow native call, pinning objects will degrade GC performance and may cause erratic performance in multithreaded applications. In these situations, it's better to rely on P/Invoke or to use unmanaged memory allocations and make copies.
