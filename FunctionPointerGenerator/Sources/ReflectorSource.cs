using System;
using System.Text;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FunctionPointerGenerator.Sources
{
    public struct ReflectorTranslationOptions
    {
        public WordType PreferredWord { get; set; }

        public bool PreserveTypeNames { get; set; }

        public bool PreserveByRef { get; set; }

        public static ReflectorTranslationOptions Default => new ReflectorTranslationOptions()
        {
            PreferredWord = WordType.IntPtr,
            PreserveByRef = true,
            PreserveTypeNames = false
        };
    }

    public enum WordType
    {
        IntPtr,
        Nint,        
    }

    public abstract class ReflectorSource : ISource
    {
        protected readonly ReflectorTranslationOptions options;
        protected ReflectorSource(ReflectorTranslationOptions options)
        {
            this.options = options;
        }

        protected string GetCallingConvention(Type type)
        {
            CallingConvention? cc = type.GetCustomAttribute<UnmanagedFunctionPointerAttribute>()?.CallingConvention;
            return ConvToString(cc);
        }

        
        protected string GetCallingConvention(MethodInfo method)
        {           
            CallingConvention? cc = method.GetCustomAttribute<DllImportAttribute>()?.CallingConvention;
            return ConvToString(cc);
        }

        protected static string ConvToString(CallingConvention? cc)
        {
            return cc switch
            {
                CallingConvention.Cdecl => "Cdecl",
                CallingConvention.StdCall => "Stdcall",
                CallingConvention.FastCall => "Fastcall",
                CallingConvention.ThisCall => "Thiscall",
                _ => null
            };
        }

        protected string GetTypeName(Type type)
        {
            var name = type.FullName;

            if (type.BaseType == typeof(MulticastDelegate))
            {                 
                return "void*"; // <-- a permanent temporary solution to callback terror
            }
            else if (name.Contains('+')) // type.IsNested is always false if the type is ref or pointer of a nested type 🤔
            {
                name = name.Replace($"{type.Namespace}.", null);
                name = name.Replace('+', '.');
            }
            else
            {
                name = type.Name;
            }

                         
            if (!this.options.PreserveTypeNames && type.Namespace.Equals("System"))
            {
                name = name.Replace(nameof(Boolean), "bool");
                name = name.Replace(nameof(Byte), "byte");
                name = name.Replace(nameof(SByte), "sbyte");
                name = name.Replace(nameof(UInt16), "ushort");
                name = name.Replace(nameof(Int16), "short");
                name = name.Replace(nameof(UInt32), "uint");
                name = name.Replace(nameof(Int32), "int");
                name = name.Replace(nameof(UInt64), "ulong");
                name = name.Replace(nameof(Int64), "long");
                name = name.Replace(nameof(Single), "float");
                name = name.Replace(nameof(Double), "double");
                name = name.Replace(nameof(Decimal), "decimal");
                name = name.Replace(nameof(String), "byte*");
                name = name.Replace(nameof(StringBuilder), "byte*");
                name = name.Replace("string", "byte*");
                name = name.Replace(nameof(Char), "char");
                name = name.Replace("Void", "void");

                if (this.options.PreferredWord == WordType.Nint)
                {
                    name = name.Replace(nameof(IntPtr), "nint");
                    name = name.Replace(nameof(UIntPtr), "nuint");
                }                                  
            }

            if(type.IsArray)
            {
                name = name.Replace("[]", "*");
            }

            return name;            
        }

        protected string GetParameterTypeName(ParameterInfo parm)
        {
            var type = parm.ParameterType;

            if (type.IsByRef)
            {
                if (this.options.PreserveByRef) 
                {
                    if (parm.IsIn)
                    {
                        return $"in {this.GetTypeName(type).TrimEnd('&')}";
                    }
                    else if (parm.IsOut)
                    {                        
                        return $"out {this.GetTypeName(type).TrimEnd('&')}";
                    }
                    else
                    {
                        return $"ref {this.GetTypeName(type).TrimEnd('&')}";
                    }
                }
                else
                {   // erase to pointers if we aren't preserving by reference         
                    return this.GetTypeName(type).Replace('&', '*'); // type.MakePointerType() fails on byref types 🤔
                }
            }
            return this.GetTypeName(type);
        }

        protected ParameterSignature[] GetParameterSignatures(ParameterInfo[] parms)
        {
            var signatures = new ParameterSignature[parms.Length];

            for(int arg = 0; arg < parms.Length; arg++)
            {
                var parm = parms[arg];
                signatures[arg] = new ParameterSignature(this.GetParameterTypeName(parm), parm.Name);                
            }

            return signatures;
        }

        public abstract void Reset();

        public abstract FunctionSignature GetNextFunction();
    }
}