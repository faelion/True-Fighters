using System;

namespace Shared
{
    public static class MathUtil
    {
        public static float Length(float x, float y)
        {
            return (float)Math.Sqrt(x * x + y * y);
        }

        public static float Distance(float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0; float dy = y1 - y0;
            return Length(dx, dy);
        }

        public static float DistanceSq(float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0; float dy = y1 - y0;
            return dx * dx + dy * dy;
        }

        // Normalizes (x,y) in place; returns original length. If too small, sets to (1,0).
        public static float Normalize(ref float x, ref float y)
        {
            float len = Length(x, y);
            if (len <= 0.0001f)
            {
                x = 1f; y = 0f; return 1f;
            }
            x /= len; y /= len; return len;
        }
    }
}

