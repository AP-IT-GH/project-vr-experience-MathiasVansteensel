using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathiasCode
{
    public struct ProportionalActionData<T>
    {
        public T Gain { get; }
        public T Error { get; }
        public T Min { get; }
        public T Max { get; }

        internal ProportionalActionData(T gain, T error, T min, T max)
        {
            Gain = gain;
            Error = error;
            Min = min;
            Max = max;
        }
    }
}