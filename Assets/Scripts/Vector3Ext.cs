using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

namespace MathiasCode
{
    //WHY THE FUCK CANT YOU DO vec * vec TF IS THIS SHIT, now i have to do it myself
    //and why cant i make an operator for a class outsize it...
    internal static class Vector3Ext
    {
        public static Vector3 Mul(this Vector3 a, Vector3 b) => new(a.x * b.x, a.y * b.y, a.z * b.z);
        public static Vector3 Div(this Vector3 a, Vector3 b) => new(a.x / b.x, a.y / b.y, a.z / b.z);
        public static Vector3 Pow(this Vector3 val, Vector3 exp) 
        {
            float x = Mathf.Pow(val.x, exp.x);
            float y = Mathf.Pow(val.y, exp.y);
            float z = Mathf.Pow(val.z, exp.z);
            return new(x, y, z);
        }

        public static void Clamp(this Vector3 a, Vector3 min, Vector3 max)
        {
            a.x = Math.Clamp(a.x, min.x, max.x);
            a.y = Math.Clamp(a.y, min.y, max.y);
            a.z = Math.Clamp(a.z, min.z, max.z);
        }

        public static Vector3 Clamped(this Vector3 a, Vector3 min, Vector3 max, out bool xClamp, out bool yClamp, out bool zClamp)
        { 
            float x = (xClamp = a.x > max.x) ? max.x : ((xClamp = a.x < min.x) ? min.x : a.x);
            float y = (yClamp = a.y > max.y) ? max.y : ((yClamp = a.y < min.y) ? min.y : a.y);
            float z = (zClamp = a.z > max.z) ? max.z : ((zClamp = a.z < min.z) ? min.z : a.z);
            return new(x, y, z);
        }
    }
}