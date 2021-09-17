using System;

namespace FunctionPointerGenerator
{
    public enum Scope
    {
        Static = 0,
        Instance
    }

    public enum FunctionLoader
    {
        None = 0,
        Function,
        Constructor,        
    }

    public struct GeneratorSettings
    {
        public string ClassName { get; set; }

        public string Namespace { get; set; }

        public Scope Scope { get; set; }

        public FunctionLoader Loader { get; set; }

        public bool AggressiveInline { get; set; }

        public string CallingConvention { get; set; }

        public string Newline { get; set; }

        public byte Identation { get; set; }

        public GeneratorSettings(string nameSpace, string className)
        {
            this.AggressiveInline = true;
            this.Scope = Scope.Static;
            this.Loader = FunctionLoader.Function;
            this.Identation = 0;
            this.CallingConvention = null;
            this.Namespace = nameSpace;
            this.ClassName = className;
            this.Newline = null;
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.ClassName)) { throw new ArgumentNullException(); }
            if (string.IsNullOrWhiteSpace(this.Namespace)) { throw new ArgumentNullException(); }
            if (string.IsNullOrWhiteSpace(this.Newline)) { this.Newline = Environment.NewLine; }
        }
    }
}