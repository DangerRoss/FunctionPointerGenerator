using System;
using System.IO;

namespace FunctionPointerGenerator
{
    public sealed class Generator
    {
        private const string pointerNameProfix = "Ptr";
        private const string functionLoaderName = "LoadFunction";

        private readonly GeneratorSettings settings;
        private readonly ISource source;
        private readonly TextWriter output;

        private readonly string identation;

        public Generator(ISource source, TextWriter output, GeneratorSettings settings)
        {
            settings.Validate();
            this.settings = settings;
            this.source = source;
            this.output = output;
            this.output.NewLine = settings.Newline;

            if(settings.Identation > 0)
            {
                // Space Fans
                Span<char> spaces = stackalloc char[settings.Identation];
                spaces.Fill(' ');
                this.identation = new string(spaces);
            }
            else // Vs
            {
                // Tab Enjoyers
                this.identation = "\t";                
            }
        }

        public void Process()
        {
            this.WriteNamespaceAndClass();

            this.WriteFunctionPointers();
            
            if(this.settings.Loader != FunctionLoader.None)
            {
                this.output.WriteLine();
                this.source.Reset();
                this.WriteFunctionLoader();
            }

            this.output.WriteLine();
            this.source.Reset();
            this.WriteFunctionCalls();
               
            this.WriteEndOfSource();
        }

        private void WriteFunctionPointers()
        {
            var twoIdent = string.Concat(this.identation, this.identation);
            var signature = this.source.GetNextFunction();

            while (signature != null)
            {
                this.output.Write(twoIdent);
                this.output.Write(this.settings.Scope == Scope.Static ? "private static " : "private ");
                signature.WritePointerType(this.output, this.settings.CallingConvention);
                this.output.WriteLine($" {signature.Name}{pointerNameProfix};");

                signature = this.source.GetNextFunction();
            }
        }

        private void WriteFunctionCalls()
        {
            var twoIdent = string.Concat(this.identation, this.identation);
            var threeIdent = string.Concat(twoIdent, this.identation);
            var signature = this.source.GetNextFunction();

            var qualifier = this.settings.Scope switch
            {
                Scope.Instance => "this",
                Scope.Static => this.settings.ClassName,
                _ => throw new NotImplementedException()
            };

            while (signature != null)
            {
                this.output.Write(twoIdent);

                if (this.settings.AggressiveInline)
                {
                    this.output.WriteLine("[MethodImpl(MethodImplOptions.AggressiveInlining)]");
                    this.output.Write(twoIdent);
                }

                this.output.Write("public ");
                signature.WriteFunctionSignature(this.output, this.settings.Scope);
                this.output.WriteLine();
                this.output.Write(twoIdent);
                this.output.WriteLine("{");
                this.output.Write(threeIdent);

                if (!signature.ReturnType.Equals("void"))
                {
                    this.output.Write("return ");
                }

                this.output.Write($"{qualifier}.{signature.Name}{pointerNameProfix}(");

                if(signature.Parameters != null)
                {
                    var upperbound = signature.Parameters.GetUpperBound(0);

                    for (int arg = 0; arg <= upperbound; arg++)
                    {
                        this.output.Write(signature.Parameters[arg].Name);
                        if(arg < upperbound)
                        {
                            this.output.Write(", ");
                        }
                    }
                }

                this.output.WriteLine(");");            
                this.output.Write(twoIdent);
                this.output.WriteLine("}");
                this.output.WriteLine();

                signature = this.source.GetNextFunction();
            }
        }

        private void WriteFunctionLoader()
        {
            var twoIdent = string.Concat(this.identation, this.identation);
            var threeIdent = string.Concat(twoIdent, this.identation);


            this.output.Write(twoIdent);
            if (this.settings.Loader == FunctionLoader.Function)
            {                
                this.output.WriteLine(this.settings.Scope == Scope.Static ? "public static void Init()" : "public void Init()");
            }
            else
            {
                this.output.WriteLine(this.settings.Scope == Scope.Static ? $"static {this.settings.ClassName}()" : $"public {this.settings.ClassName}()");
            }
            this.output.Write(twoIdent);
            this.output.WriteLine("{");

            var signature = this.source.GetNextFunction();

            var qualifier = this.settings.Scope switch
            {
                Scope.Instance => "this",
                Scope.Static => this.settings.ClassName,
                _ => throw new NotImplementedException()
            };

            while(signature != null)
            {
                this.output.Write(threeIdent);
                this.output.Write($"{qualifier}.{signature.Name}{pointerNameProfix}");
                this.output.Write(" = (");
                signature.WritePointerType(this.output, this.settings.CallingConvention);
                this.output.WriteLine($"){qualifier}.{functionLoaderName}(\"{signature.Name}\");");

                signature = this.source.GetNextFunction();
            }

            this.output.Write(twoIdent);
            this.output.WriteLine("}");


            this.output.WriteLine();
            this.output.Write(twoIdent);
            this.output.Write("private ");
            if(settings.Scope == Scope.Static)
            {
                this.output.Write("static ");
            }
            this.output.WriteLine($"void* {functionLoaderName}(string name)");
            this.output.Write(twoIdent);
            this.output.WriteLine("{");
            this.output.Write(threeIdent);
            this.output.WriteLine("// Provide a function loader pls");
            this.output.Write(threeIdent);
            this.output.WriteLine("throw new NotImplementedException();");
            this.output.Write(twoIdent);
            this.output.WriteLine("}");
            this.output.WriteLine();
        }

        private void WriteNamespaceAndClass()
        {
            this.output.WriteLine("using System;");

            if(this.settings.AggressiveInline)
            {
                this.output.WriteLine("using System.Runtime.CompilerServices;");
            }
            
            this.output.WriteLine();
            this.output.WriteLine($"namespace {this.settings.Namespace}");
            this.output.WriteLine("{");
            this.output.Write(this.identation);

            if(this.settings.Scope == Scope.Static)
            {
                this.output.Write("static ");
            }
            this.output.WriteLine($"unsafe class {this.settings.ClassName}");
            this.output.Write(this.identation);
            this.output.WriteLine("{");
        }

        private void WriteEndOfSource()
        {
            this.output.Write(this.identation);
            this.output.WriteLine("}");
            this.output.Write("}");
        }
    }
}