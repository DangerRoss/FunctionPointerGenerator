using System.Collections.Generic;

namespace FunctionPointerGenerator
{
    public interface ISource
    {
        void Reset();

        FunctionSignature GetNextFunction();
    }

    public static class ISourceExtensions
    {
        public static ISource ToISource(this IEnumerable<FunctionSignature> enumerable) => new EnumeratorWrapper(enumerable.GetEnumerator());

        private sealed class EnumeratorWrapper : ISource
        {
            private readonly IEnumerator<FunctionSignature> enumerator;

            public EnumeratorWrapper(IEnumerator<FunctionSignature> enumerator)
            {
                this.enumerator = enumerator;
            }

            public FunctionSignature GetNextFunction()
            {
                if(this.enumerator.MoveNext())
                {
                    return this.enumerator.Current;
                }
                else
                {
                    return null;
                }
            }

            public void Reset() => this.enumerator.Reset();
        }
    }
}