using System.IO;

namespace FunctionPointerGenerator
{
    public record FunctionSignature
    {
        public string Name { get; init; }
        public string ReturnType { get; init; }
        public string CallConvention { get; init; }
        public ParameterSignature[] Parameters { get; init; }

        public void WritePointerType(TextWriter writer, string defaultCallConvention)
        {
            writer.Write("delegate* unmanaged ");

            if (!string.IsNullOrWhiteSpace(this.CallConvention))
            {
                writer.Write($"[{this.CallConvention}] ");
            }
            else if (!string.IsNullOrWhiteSpace(defaultCallConvention))
            {
                writer.Write($"[{defaultCallConvention}] ");
            }

            writer.Write("<");

            if(this.Parameters != null)
            {
                for (int arg = 0; arg < Parameters.Length; arg++)
                {
                    writer.Write(this.Parameters[arg].Type);
                    writer.Write(", ");
                }
            }

            writer.Write(this.ReturnType);
            writer.Write(">");
        }

        public void WriteFunctionSignature(TextWriter writer, Scope scope)
        {
            if(scope == Scope.Static)
            {
                writer.Write("static ");
            }
            writer.Write($"{this.ReturnType} {this.Name}(");

            if (this.Parameters != null)
            {
                var upperbound = this.Parameters.GetUpperBound(0);
                for (int arg = 0; arg <= upperbound; arg++)
                {
                    var parm = this.Parameters[arg];
                    writer.Write($"{parm.Type} {parm.Name}");

                    if(arg < upperbound)
                    {
                        writer.Write(", ");
                    }                    
                }
            }
            writer.Write(")");
        }
    }
        
    public readonly struct ParameterSignature
    {
        public readonly string Type;
        public readonly string Name;

        public ParameterSignature(string type, string name)
        {
            this.Type = type;
            this.Name = name;
        }
    }
}