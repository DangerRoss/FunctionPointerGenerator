# Function Pointer Generator (FPG)

FPG is a primitive code generator tool for C# intended for creating shared library function declarations using C# 9.0 function pointers. Function definitions to generate source code for are provided by either a text file of function signatures, or read from a .NET assembly containing PInvoke functions or Delegates. 

## Example

Below is an example of generated source code using function signature list for three Win32 functions; [GetProcessHeap](https://docs.microsoft.com/en-us/windows/win32/api/heapapi/nf-heapapi-getprocessheap), [HeapAlloc](https://docs.microsoft.com/en-us/windows/win32/api/heapapi/nf-heapapi-heapalloc) and [HeapFree](https://docs.microsoft.com/en-us/windows/win32/api/heapapi/nf-heapapi-heapfree).

**FuncList.txt**

```
IntPtr GetProcessHeap()
void HeapAlloc(IntPtr hHeap, uint dwFlags, nuint dwBytes)
bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem)
```

**Command**

```
fpg --txtlist "Funclist.txt" namespace=Win32 class=Kernel32 scope=instance loader=constructor
```

**Generated Source Code**

````csharp
using System;
using System.Runtime.CompilerServices;

namespace Win32
{
    unsafe class Kernel32
    {
        private delegate* unmanaged<IntPtr> GetProcessHeapPtr;
        private delegate* unmanaged<IntPtr, uint, nuint, void> HeapAllocPtr;
        private delegate* unmanaged<IntPtr, uint, IntPtr, bool> HeapFreePtr;
        
        public Kernel32()
        {
            this.GetProcessHeapPtr = (delegate* unmanaged<IntPtr>)this.LoadFunction("GetProcessHeap");
            this.HeapAllocPtr = (delegate* unmanaged<IntPtr, uint, nuint, void>)this.LoadFunction("HeapAlloc");
            this.HeapFreePtr = (delegate* unmanaged<IntPtr, uint, IntPtr, bool>)this.LoadFunction("HeapFree");
        }
        
        private void* LoadFunction(string name)
        {
            // placeholder for user-provided function loader
            throw new NotImplementedException();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IntPtr GetProcessHeap()
        {
            return this.GetProcessHeapPtr();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void HeapAlloc(IntPtr hHeap, uint dwFlags, nuint dwBytes)
        {
            this.HeapAllocPtr(hHeap, dwFlags, dwBytes);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem)
        {
            return this.HeapFreePtr(hHeap, dwFlags, lpMem);
        }
    }
}
````

## Usage

FPG can generate function calls from a couple of different sources including .NET assemblies. All commands are proceeded by a ordered number of arguments then followed by common parameters as follows:

```
--command arg1 arg2 parameters
```

### Commands

- `--txtlist filepath`  Reads a text file of function signatures to export. 

- `--pinvoke assembly qualifier` Parses a .NET assembly for pinvoke functions to export. The qualifier is used to specify what namespace to include in searching for pinvoke functions. To include the entire assembly use `*`.
- `--delegate assembly qualifier` Parses a .NET assembly for delegate types to export. The qualifier behaves the same as it does for the pinvoke command.

### Parameters

| Name             | Value                                                        | Mandatory | Example                |
| ---------------- | ------------------------------------------------------------ | --------- | ---------------------- |
| `namespace`      | The namespace the generated class will be in                 | Required  | `namespace=Win32`      |
| `class`          | The name of the generated class                              | Required  | `class=Kernel32`       |
| `scope`          | `static instance` Specifies the generated class to be static or instance. Default is static. | Optional  | `scope=static`         |
| `loader`         | `constructor function none` Specifies if the generated class includes function loading template using function, constructor or omitted. Default is function. | Optional  | `loader=function`      |
| `callconv`       | Declares default call convention to use when a function doesn't specify. If there is no default provided and a function doesn't specify, the function pointer has its call convention omitted and will use the preferred convention of the target platform. | Optional  | `callconv=Stdcall`     |
| `indentation`    | Controls the indentation used in source generation. A non-zero value specifies number of spaces and a zero value specifies single tabs which is the default. | Optional  | `identation=4`         |
| `out`            | Redirects the output to a specified file                     | Optional  | `out="Kernel32.cs"`    |
| `wordtype`       | `nint IntPtr` determines which type to use when handling words. Only applicable for --delegate and --pinvoke commands. Default is `IntPtr` | Optional  | `wordtype=nint`        |
| `preservebyrefs` | `true false` decides to preserve by reference parameters. When false, all by reference parameters are exported as pointers. Only applicable for --delegate and --pinvoke commands. Default is `true` | Optional  | `preservebyrefs=false` |
| `preservetypes`  | `true false` decides to export types qualified exactly as they are defined. When `false` certain types such as primitives are exported as their reserved keywords rather than runtime type names. This also overrides the word type parameter when `true`. Only applicable for --delegate and --pinvoke commands. Default is `false` | Optional  | `preservetypes=true`   |









