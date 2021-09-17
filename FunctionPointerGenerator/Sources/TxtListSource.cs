using System;
using System.IO;

namespace FunctionPointerGenerator.Sources
{
    public sealed class TxtListSource : ISource
    {
        private readonly StreamReader reader;
        public TxtListSource(StreamReader reader)
        {
            this.reader = reader;
        }

        public FunctionSignature GetNextFunction()
        {
            var line = this.reader.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(line))
            {
                return null;
            }

            var firstSpace = line.IndexOf(' ');
            var parmStart = line.IndexOf('(');
            var parmEnd = line.LastIndexOf(")");

            var type = line.Substring(0, firstSpace);
            var name = line.Substring(firstSpace + 1, parmStart - (firstSpace + 1));
            var conv = (line.Length - 1 == parmEnd) ? null : line.Substring(parmEnd + 1).Trim();
            var parameters = Array.Empty<ParameterSignature>();

            var parmsstr = line.Substring(parmStart + 1, parmEnd - parmStart - 1).Trim();

            if (!string.IsNullOrWhiteSpace(parmsstr) && !parmsstr.Equals("void"))
            {
                if(parmsstr.Contains(','))
                {
                    var parmsSplit = parmsstr.Split(',');
                    parameters = new ParameterSignature[parmsSplit.Length];

                    for(int arg = 0; arg < parameters.Length; arg++)
                    {
                        var pstr = parmsSplit[arg].Trim();
                        var pstrSplit = pstr.Split(' ');

                        if(parmsstr.Length == 3) // we assume the first value is a byref modifier
                        {                            
                            var refmod = pstrSplit[0].Trim();
                            if(refmod.Equals("in") || refmod.Equals("ref") || refmod.Equals("out"))
                            {
                                var reftype = string.Concat(refmod, " ", pstrSplit[1].Trim());
                                parameters[arg] = new ParameterSignature(reftype, pstrSplit[2].Trim());
                            }
                            else
                            {
                                throw new FormatException();
                            }                            
                        }
                        else
                        {
                            parameters[arg] = new ParameterSignature(pstrSplit[0].Trim(), pstrSplit[1].Trim());
                        }                        
                    }
                }
                else
                {
                    parameters = new ParameterSignature[1];
                    var pstrSplit = parmsstr.Split(' ');
                    parameters[0] = new ParameterSignature(pstrSplit[0].Trim(), pstrSplit[1].Trim());
                }
            }

            return new FunctionSignature()
            {
                Name = name,
                ReturnType = type,
                CallConvention = conv,
                Parameters = parameters
            };            
        }

        public void Reset()
        {
            this.reader.BaseStream.Seek(0, SeekOrigin.Begin);
            this.reader.DiscardBufferedData();
        }
    }
}
