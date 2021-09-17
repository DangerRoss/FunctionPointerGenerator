using System;
using System.IO;
using System.Reflection;

using FunctionPointerGenerator.Sources;

namespace FunctionPointerGenerator
{
    sealed class App
    {
        private TextWriter output;

        private const string helpCommand = "--help";
        private const string txtListCommand = "--txtlist";
        private const string delegateCommand = "--delegate";
        private const string pinvokeCommand = "--pinvoke";

        private const string classParam = "class";
        private const string namespaceParam = "namespace";
        private const string indentationParam = "indentation";
        private const string callconvParam = "callconv";
        private const string scopeParam = "scope";
        private const string loaderParam = "loader";
        private const string outputParam = "out";

        private const string wordTypeParam = "wordtype";
        private const string preserveRefsParam = "preservebyrefs";
        private const string preserveTypesParam = "preservetypes";

        static void Main(string[] args)
        {
            var app = new App(Console.Out);
            app.ProcessCommands(args);
        }

        public App(TextWriter output)
        {
            this.output = output;                        
        }

        private void ExitWithMissingParameters()
        {
            this.output.WriteLine("missing parameters");
            this.output.WriteLine($"use {helpCommand} for command list");
        }

        public void ProcessCommands(string[] args)
        {
            if(args.Length == 0)
            {
                this.ExitWithMissingParameters();   
                return;
            }

            var command = args[0].Trim();

            if(command.Equals(helpCommand, StringComparison.OrdinalIgnoreCase))
            {
                this.ListCommands();
            }
            else if (command.Equals(txtListCommand, StringComparison.OrdinalIgnoreCase))
            {
                this.ProcessTextList(args);
            }
            else if (command.Equals(delegateCommand, StringComparison.OrdinalIgnoreCase))
            {
                this.ProcessDelegates(args);
            }
            else if (command.Equals(pinvokeCommand, StringComparison.OrdinalIgnoreCase))
            {
                this.ProcessPInvokes(args);
            }
            else
            {
                this.output.WriteLine("unknown command");
                this.output.WriteLine($"use {helpCommand} for command list");
            }
        }

        private void ListCommands()
        {
            output.WriteLine(helpCommand);

            output.Write($"{txtListCommand} [filepath] ");
            this.ListParameters();

            output.Write($"{delegateCommand} [assembly] [qualifier] ");
            this.ListParameters();
            this.ListReflectorParameters();

            output.Write($"{pinvokeCommand} [assembly] [qualifier] ");
            this.ListParameters();
            this.ListReflectorParameters();

        }

        private void ListParameters()
        {
            output.Write($"{namespaceParam}=[value] ");
            output.Write($"{classParam}=[value] ");
            output.Write($"{callconvParam}=[value] ");
            output.Write($"{indentationParam}=[0-255] ");
            output.Write($"{scopeParam}=[static|instance] ");
            output.Write($"{loaderParam}=[constructor|function|none] ");
            output.WriteLine($"{outputParam}=[filepath]");
        }

        private void ListReflectorParameters()
        {
            output.Write($"{wordTypeParam}=[nint|IntPtr] ");
            output.Write($"{preserveRefsParam}=[true|false] ");
            output.Write($"{preserveTypesParam}=[true|false] ");
        }

        private GeneratorSettings ProcessParameters(string[] args, int startarg, out string output)
        {
            var settings = new GeneratorSettings(null, null);
            output = null;
            for (int parmIndex = startarg; parmIndex < args.Length; parmIndex++)
            {
                var parm = args[parmIndex];
                var sepIndex = parm.IndexOf('=');

                if (sepIndex > -1)
                {
                    var pname = parm.Substring(0, sepIndex).Trim();
                    var pvalue = parm.Substring(sepIndex + 1).Trim();

                    if (pname.Equals(namespaceParam, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.Namespace = pvalue;
                    }
                    else if (pname.Equals(classParam, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.ClassName = pvalue;
                    }
                    else if (pname.Equals(callconvParam, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.CallingConvention = pvalue;
                    }
                    else if (pname.Equals(indentationParam, StringComparison.OrdinalIgnoreCase))
                    {
                        settings.Identation = byte.Parse(pvalue);
                    }
                    else if (pname.Equals(scopeParam, StringComparison.OrdinalIgnoreCase))
                    {
                        if (pvalue.Equals("static", StringComparison.OrdinalIgnoreCase))
                        {
                            settings.Scope = Scope.Static;
                        }
                        else if (pvalue.Equals("instance", StringComparison.OrdinalIgnoreCase))
                        {
                            settings.Scope = Scope.Instance;
                        }
                    }
                    else if (pname.Equals(loaderParam, StringComparison.OrdinalIgnoreCase))
                    {
                        if (pvalue.Equals("constructor", StringComparison.OrdinalIgnoreCase))
                        {
                            settings.Loader = FunctionLoader.Constructor;
                        }
                        else if (pvalue.Equals("function", StringComparison.OrdinalIgnoreCase))
                        {
                            settings.Loader = FunctionLoader.Function;
                        }
                        else
                        {
                            settings.Loader = FunctionLoader.None;
                        }
                    }
                    else if (pname.Equals(outputParam, StringComparison.OrdinalIgnoreCase))
                    {
                        output = pvalue;
                    }
                }
            }

            return settings;
        }

        private ReflectorTranslationOptions ProcessReflectorParameters(string[] args, int startarg)
        {
            var rto = ReflectorTranslationOptions.Default;
            
            for (int parmIndex = startarg; parmIndex < args.Length; parmIndex++)
            {
                var parm = args[parmIndex];
                var sepIndex = parm.IndexOf('=');

                if (sepIndex > -1)
                {
                    var pname = parm.Substring(0, sepIndex).Trim();
                    var pvalue = parm.Substring(sepIndex + 1).Trim();

                    if (pname.Equals(wordTypeParam, StringComparison.OrdinalIgnoreCase))
                    {
                        rto.PreferredWord = pvalue.ToLower().Equals("nint") ? WordType.Nint : WordType.IntPtr;
                    }
                    else if (pname.Equals(preserveRefsParam, StringComparison.OrdinalIgnoreCase))
                    {
                        if(bool.TryParse(pvalue, out var result))
                        {
                            rto.PreserveByRef = result;
                        }
                    }
                    else if (pname.Equals(preserveTypesParam, StringComparison.OrdinalIgnoreCase))
                    {
                        if (bool.TryParse(pvalue, out var result))
                        {
                            rto.PreserveTypeNames = result;
                        }
                    }
                }
            }

            return rto;
        }

        private void Generate(ISource source, GeneratorSettings settings, string outputpath)
        {
            if (string.IsNullOrEmpty(outputpath))
            {
                var generator = new Generator(source, Console.Out, settings);
                generator.Process();
            }
            else
            {
                using var outFile = new FileStream(outputpath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                using var outStream = new StreamWriter(outFile);
                var generator = new Generator(source, outStream, settings);
                generator.Process();
            }
        }

        private void ProcessTextList(string[] args)
        {
            if(args.Length < 4)
            {
                this.ExitWithMissingParameters();
                return;
            }

            var path = args[1];

            var settings = this.ProcessParameters(args, 2, out var outValue);

            using var file = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var txtStream = new StreamReader(file);
            var source = new TxtListSource(txtStream);

            this.Generate(source, settings, outValue);   
        }

        private void ProcessDelegates(string[] args)
        {
            if (args.Length < 5)
            {
                this.ExitWithMissingParameters();
                return;
            }

            var asmpath = args[1];
            var namespaceFilter = args[2];

            var settings = this.ProcessParameters(args, 3, out var outValue);
            var translationOptions = this.ProcessReflectorParameters(args, 3);

            var source = new DelegateSource(Assembly.LoadFrom(asmpath), namespaceFilter, translationOptions);

            this.Generate(source, settings, outValue);
        }

        private void ProcessPInvokes(string[] args)
        {
            if (args.Length < 5)
            {
                this.ExitWithMissingParameters();
                return;
            }

            var asmpath = args[1];
            var namespaceFilter = args[2];

            var settings = this.ProcessParameters(args, 3, out var outValue);
            var translationOptions = this.ProcessReflectorParameters(args, 3);

            var source = new PInvokeSource(Assembly.LoadFrom(asmpath), namespaceFilter, translationOptions);

            this.Generate(source, settings, outValue);
        }
    }
}