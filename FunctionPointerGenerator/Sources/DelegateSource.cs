using System;
using System.Linq;
using System.Reflection;

namespace FunctionPointerGenerator.Sources
{
    sealed class DelegateSource : ReflectorSource
    {
        private readonly Type[] types;
        private int current;

        public DelegateSource(Assembly assembly, string searchPath, ReflectorTranslationOptions options) : base(options)
        {
            if(string.IsNullOrWhiteSpace(searchPath) || searchPath.Trim().Equals("*"))
            {
                this.types = (from Type t in assembly.GetTypes() 
                              where t.BaseType == typeof(MulticastDelegate) && !t.IsGenericType
                              select t).ToArray();
            }
            else
            {
                var filter = searchPath.Trim().Trim('*');
                this.types = (from Type t in assembly.GetTypes()
                              where t.BaseType == typeof(MulticastDelegate) && !t.IsGenericType && t.FullName.StartsWith(filter)
                              select t).ToArray();
            }
            this.Reset();
        }

        public override void Reset() => this.current = -1;

        public override FunctionSignature GetNextFunction()
        {
            var current = this.current + 1;

            if(current >= this.types.Length)
            {
                return null;
            }

            var type = this.types[current];
            var invokeMethod = type.GetMethod("Invoke");
            var signature = new FunctionSignature()
            {
                Name = type.Name,
                ReturnType = this.GetTypeName(invokeMethod.ReturnType),
                CallConvention = this.GetCallingConvention(type),
                Parameters = this.GetParameterSignatures(invokeMethod.GetParameters())
            };

            this.current = current;
            return signature;
        }        
    }
}