using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MathiasCode
{
    //Source: me... bro all my friends are automation engineers why would you think i dont know how a PID works
    //this is not very scalable, making a seperate exact copy of a class with a different type... but whatever
    public class PID3D
    {
        public Vector3 ProportionalGain { get; set; }
        public Vector3 IntegralGain { get; set; }
        public Vector3 DifferentialGain { get; set; }
        public Vector3 Setpoint { get; set; }
        public Vector3 ProcessValue { get; set; }
        public Vector3 PMax { get; set; }
        public Vector3 IMax { get; set; }
        public Vector3 DMax { get; set; }

        public Vector3? PMin { get; set; }
        public Vector3? IMin { get; set; }
        public Vector3? DMin { get; set; }

        public Vector3 LastError { get; set; } = Vector3.zero;

        //public float? MaxDAngle { get; set; }

        public Func<ProportionalActionData<Vector3>, Vector3> PActionOverride { get; set; } = null;
        public Func<IntegralActionData<Vector3>, Vector3> IActionOverride { get; set; } = null;
        public Func<DifferentialActionData<Vector3>, Vector3> DActionOverride { get; set; } = null;

        private Vector3 integralValue = Vector3.zero;
        private bool isFirstTick = true;

        public PID3D(PID3DSettings settings)
        {
            ProportionalGain = settings.ProportionalGain;
            IntegralGain = settings.IntegralGain;
            DifferentialGain = settings.DifferentialGain;
            PMax = settings.PMax;
            IMax = settings.IMax;
            DMax = settings.DMax;
            PMin = -PMax;
            IMin = -IMax;
            DMin = -DMax;
        }

        public PID3D(Vector3 proportionalGain, Vector3 integralGain, Vector3 differentialGain, Vector3 pMax, Vector3 iMax, Vector3 dMax, Vector3? pMin = null, Vector3? iMin = null, Vector3? dMin = null, Vector3? maxDAngle = null)
        {
            ProportionalGain = proportionalGain;
            IntegralGain = integralGain;
            DifferentialGain = differentialGain;
            PMax = pMax;
            IMax = iMax;
            DMax = dMax;
            PMin = pMin ?? -PMax;
            IMin = iMin ?? -IMax;
            DMin = dMin ?? -DMax;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 Tick(out Vector3 p, out Vector3 i, out Vector3 d, out Vector3 error)
        {
            error = ProcessValue - Setpoint;
            if (isFirstTick)
            {
                isFirstTick = false;
                LastError = error;
                p = Vector3.zero;
                i = Vector3.zero;
                d = Vector3.zero;
                return Vector3.zero;
            }

            ProportionalActionData<Vector3> pd = new(ProportionalGain, error, PMin.Value, PMax);
            IntegralActionData<Vector3> id = new(IntegralGain, error, integralValue, IMin.Value, IMax);
            DifferentialActionData<Vector3> dd = new(DifferentialGain, error, LastError, DMin.Value, DMax);

            p = PActionOverride?.Invoke(pd) ?? PAction(pd);
            integralValue = i = IActionOverride?.Invoke(id) ?? IAction(id);
            d = DActionOverride?.Invoke(dd) ?? DAction(dd);
            LastError = error;

            return -(p + i + d);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 PAction(ProportionalActionData<Vector3> data) => data.Gain.Mul(data.Error).Clamped(data.Min, data.Max, out _, out _, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 IAction(IntegralActionData<Vector3> data)
        {
            Vector3 integral = data.IntegralValue;
            if (integral.x <= data.Max.x)
                integral.x = Math.Clamp(integral.x + (data.Gain.x * data.Error.x), data.Min.x, data.Max.x);

            if (integral.y <= data.Max.y)
                integral.y = Math.Clamp(integral.y + (data.Gain.y * data.Error.y), data.Min.y, data.Max.y);

            if (integral.z <= data.Max.z)
                integral.z = Math.Clamp(integral.z + (data.Gain.z * data.Error.z), data.Min.z, data.Max.z);
            return integral;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Vector3 DAction(DifferentialActionData<Vector3> data)
        {
            //TODO: max angle :)
            Vector3 differential = data.Error - data.LastError;
            Vector3 d = data.Gain.Mul(differential).Clamped(data.Min, data.Max, out _, out _, out _);
            return d;
        }
    }
}
