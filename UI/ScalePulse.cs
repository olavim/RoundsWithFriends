using System.Collections;
using UnityEngine;

namespace RWF.UI
{
    class ScalePulse : MonoBehaviour
    {
        private bool isPulsating = false;

        public IEnumerator StartPulse(float scale = 1.2f, float duration = 0.1f, float delay = 0.5f) {
            this.isPulsating = true;

            while (this.isPulsating) {
                float t = 0f;
                this.gameObject.transform.localScale = Vector3.one * scale;

                while (t < 1) {
                    t += Time.deltaTime / duration;
                    this.gameObject.transform.localScale = Vector3.Lerp(this.gameObject.transform.localScale, Vector3.one, t);
                    yield return null;
                }

                this.gameObject.transform.localScale = Vector3.one;

                yield return new WaitForSeconds(delay);
            }
        }

        public void StopPulse() {
            this.isPulsating = false;
        }
    }
}
