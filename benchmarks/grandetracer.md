## DESCRIPTION
Ray tracer.

## KNOWN ISSUES

### "Validation failed"

When the test finishes, it may print the following message:
```
Validation failed
Pixel checksum = ...
Reference value = ...
```

The message, however, does not necessarily indicate a problem with the runtime. If the difference the checksum is small (<0.1%), it may have been caused by different implementations of Math.Tan(), and perhaps some other standard library functions, that different .NET runtimes on different platforms implement differently.

For instance, Mono and CoreCLR use the native function tan() to implement Math.Tan(). With OS X libc `tan(0x1.38c35412a6cbcp-1)` = `0x1.66819a311c717p-1`, however, with Glibc it's `0x1.66819a311c718p-1`. Since there is no strict requirement in C# to adhere to one specific algorithm, and requirements on precision are very vague, such discrepancy is more an issue with the benchmark which is expecting too much precision from the standard, rather than the runtimes or the standard C libraries.

For the reference, to achieve the same result as it appears with CLR on Windows, the FPTAN assembly instruction should be used. The implementation is present in Mono (see OP_TAN), but is currently disabled.

Considering the above, small deviations from the expected checksum should be taken into account, the performance results still considered valid, and the error message discarded.