using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathiasCode
{
    public struct DifferentialActionData<T>
    {
        public T Gain { get; }
        public T Error { get; }
        public T Min { get; }
        public T Max { get; }
        public T LastError { get; }

        internal DifferentialActionData(T gain, T error, T lastError, T min, T max)
        {
            Gain = gain;
            Error = error;
            Min = min;
            Max = max;
            LastError = lastError;
        }
    }
}