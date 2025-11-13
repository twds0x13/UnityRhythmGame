using UnityEngine;

namespace Vector3ExtensionsNS
{
    public static class Vector3Extensions
    {
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }

        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }

        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        public static Vector3 AddX(this Vector3 vector, float x)
        {
            return new Vector3(vector.x + x, vector.y, vector.z);
        }

        public static Vector3 AddY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, vector.y + y, vector.z);
        }

        public static Vector3 AddZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, vector.z + z);
        }

        public static Vector3 MultiplyX(this Vector3 vector, float x)
        {
            return new Vector3(vector.x * x, vector.y, vector.z);
        }

        public static Vector3 MultiplyY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, vector.y * y, vector.z);
        }

        public static Vector3 MultiplyZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, vector.z * z);
        }
    }
}
