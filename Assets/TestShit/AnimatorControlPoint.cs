using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MathiasCode
{
    [Serializable]
    public class AnimatorControlPoint
    {
        public Rigidbody ParentRigidbody;
        public Transform ForcePoint;
        public Transform TargetPoint;
        public int PidSettingIndex;
        public bool Enable = true;
    }
}
