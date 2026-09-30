using System.Collections;
using UnityEngine;

namespace SurgicalFoundations.Interaction
{
    /// <summary>
    /// Put on a step panel: when it appears, the learner's tracked hands switch to this look
    /// (e.g. bare at the scrub sink, gloved from gowning on, left glove contaminated at the sterility break).
    /// </summary>
    public class HandLookCue : MonoBehaviour
    {
        [SerializeField] HandLook left = HandLook.Gloved;
        [SerializeField] HandLook right = HandLook.Gloved;

        public void Configure(HandLook leftLook, HandLook rightLook) { left = leftLook; right = rightLook; }

        void OnEnable() => StartCoroutine(Apply());

        IEnumerator Apply()
        {
            // HandAppearance lives in 00_Bootstrap, which may still be loading when a scene is opened directly.
            while (HandAppearance.Instance == null) yield return null;
            HandAppearance.Instance.Set(left, right);
        }
    }
}
