using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MathiasCode
{
    public struct IntegralActionData<T>
    {
        public T Gain { get; }
        public T Error { get; }
        public T Min { get; }
        public T Max { get; }
        public T IntegralValue { get; }

        internal IntegralActionData(T gain, T error, T integralValue, T min, T max)
        {
            Gain = gain;
            Error = error;
            Min = min;
            Max = max;
            IntegralValue = integralValue;
        }
    }
}