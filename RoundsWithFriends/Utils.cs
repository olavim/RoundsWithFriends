using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace RWF
{

    public static class AverageColor
    {
        public static Color Average(List<Color> colors)
        {
            return AverageColor.Average(colors.ToArray());
        }
        public static Color Average(Color[] colors)
        {
            float r = 0f;
            float g = 0f;
            float b = 0f;
            float a = 0f;
            foreach (Color color in colors)
            {
                r += color.r;
                g += color.g;
                b += color.b;
                a += color.a;
            }
            float num = (float) colors.Count();
            return new Color(r / num, g / num, b / num, a / num);
        }
    }
    static class Math
    {
        public static int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }
    }

    public class Profile
    {
        public Profile(string name)
        {
            this.name = name;
            this.Start();
        }

        private float start = -1f;
        private float duration = 0f;
        private string name;

        public void Start()
        {
            this.start = Time.realtimeSinceStartup;
        }

        public void Stop()
        {
            this.duration += Time.realtimeSinceStartup - this.start;
        }

        public void Report()
        {
            UnityEngine.Debug.Log($"{this.name} Duration: {this.duration.ToString()} sec");
        }

        public void StopAndReport()
        {
            this.Stop();
            this.Report();
        }
    }
}