using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MathiasCode
{
    [Serializable]
    public class AnimatorTarget
    {
        public Rigidbody BodyPart;
        public Transform? ForcePoint;
        public GameObject Target;
        public int PidSettingIndex;
        public bool Enable = true;
    }
}
