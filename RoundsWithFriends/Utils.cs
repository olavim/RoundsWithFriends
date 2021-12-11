using UnityEngine;

namespace RWF
{
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