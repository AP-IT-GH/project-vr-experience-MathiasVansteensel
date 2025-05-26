using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MathiasCode
{
    //Source: me... bro all my friends are automation engineers why would you think i dont know how a PID works
    //Bruh i fucking tried making this generic but holy shit never mind fuck that lets never do that again until unity gets support for .NET 6+
    public class PID
    {
        public float ProportionalGain { get; set; }
        public float IntegralGain { get; set; }
        public float DifferentialGain { get; set; }
        public float Setpoint { get; set; }
        public float ProcessValue { get; set; }
        public float PMax { get; set; }
        public float IMax { get; set; }
        public float DMax { get; set; }

        public float? PMin { get; set; }
        public float? IMin { get; set; }
        public float? DMin { get; set; }

        //public float? MaxDAngle { get; set; }

        public Func<ProportionalActionData<float>, float> PActionOverride { get; set; } = null;
        public Func<IntegralActionData<float>, float> IActionOverride { get; set; } = null;
        public Func<DifferentialActionData<float>, float> DActionOverride { get; set; } = null;

        private float integralValue = 0f;
        private float lastError = 0f;
        private bool isFirstTick = true;


        public PID(float proportionalGain, float integralGain, float DifferentialGain, float pMax, float iMax, float dMax, float? pMin = null, float? iMin = null, float? dMin = null, float? maxDAngle = null)
        {
            ProportionalGain = proportionalGain;
            IntegralGain = integralGain;
            this.DifferentialGain = DifferentialGain;
            PMax = pMax;
            IMax = iMax;
            DMax = dMax;
            PMin = pMin ?? -PMax;
            IMin = iMin ?? -IMax;
            DMin = dMin ?? -DMax;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Tick(out float p, out float i, out float d)
        {
            float error = ProcessValue - Setpoint;
            if (isFirstTick)
            {
                isFirstTick = false;
                lastError = error;
                p = 0;
                i = 0;
                d = 0;
                return 0f;
            }

            ProportionalActionData<float> pd = new(ProportionalGain, error, PMin.Value, PMax);
            IntegralActionData<float> id = new(IntegralGain, error, integralValue, IMin.Value, IMax);
            DifferentialActionData<float> dd = new(DifferentialGain, error, lastError, DMin.Value, DMax);

            p = PActionOverride?.Invoke(pd) ?? PAction(pd);
            i = IActionOverride?.Invoke(id) ?? IAction(id);
            d = DActionOverride?.Invoke(dd) ?? DAction(dd);
            lastError = error;

            return -(p + i + d);
        }

        private float PAction(ProportionalActionData<float> data) => Math.Clamp(data.Gain * data.Error, data.Min, data.Max);

        private float IAction(IntegralActionData<float> data)
        {
            float integral = data.IntegralValue;
            if (integral <= data.Max)
                integral = Math.Clamp(integral + (data.Gain * data.Error), data.Min, data.Max);
            return integral;
        }

        private float DAction(DifferentialActionData<float> data)
        {
            //TODO: max angle :)
            float d = Math.Clamp(data.Gain * (data.Error - data.LastError), data.Min, data.Max);
            return d;
        }
    }
}
