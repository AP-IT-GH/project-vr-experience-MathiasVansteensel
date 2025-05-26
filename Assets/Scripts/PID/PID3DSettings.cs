using System;
using UnityEngine;

namespace MathiasCode
{
    [Serializable]
    public struct PID3DSettings
    {
        //why tf cant unity serialize properties, only fields
        public Vector3 ProportionalGain;
        public Vector3 IntegralGain;
        public Vector3 DifferentialGain;
        [Space]
        public Vector3 PMax;
        public Vector3 IMax;
        public Vector3 DMax;
    }
}
