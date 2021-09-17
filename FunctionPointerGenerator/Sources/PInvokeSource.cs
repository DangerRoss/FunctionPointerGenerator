using System;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;

namespace FunctionPointerGenerator.Sources
{
    sealed class PInvokeSource : ReflectorSource
    {
        private MethodInfo[] methods;
        private int current;
        public PInvokeSource(Assembly assembly, string searchPath, ReflectorTranslationOptions options) : base(options)
        {
            if (string.IsNullOrWhiteSpace(searchPath) || searchPath.Trim().Equals("*"))
            {
                this.methods = (from Type t in assembly.GetTypes()
                                    from MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                    where m.GetCustomAttribute<DllImportAttribute>() != null
                                    select m).ToArray();                                    
            }
            else
            {
                var filter = searchPath.Trim().Trim('*');
                this.methods = (from Type t in assembly.GetTypes() 
                                where t.FullName.StartsWith(filter)
                                from MethodInfo m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                where m.GetCustomAttribute<DllImportAttribute>() != null
                                select m).ToArray();
            }
        }

        public override void Reset() => this.current = -1;        

        public override FunctionSignature GetNextFunction()
        {
            var current = this.current + 1;

            if (current >= this.methods.Length)
            {
                return null;
            }

            var method = this.methods[current];
            var signature = new FunctionSignature()
            {
                Name = method.Name,
                ReturnType = this.GetTypeName(method.ReturnType),
                CallConvention = this.GetCallingConvention(method),
                Parameters = this.GetParameterSignatures(method.GetParameters())
            };

            this.current = current;
            return signature;
        }
    }
}