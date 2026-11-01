using UnityEngine;
using UnityEngine.UI;

namespace BattlePrep.Tent
{
    // Makes an Image only take clicks where its sprite is visible (spec TentHub.md §3). The sprite's texture must have
    // Read/Write enabled; otherwise the whole rectangle is used.
    [RequireComponent(typeof(Image))]
    public class TentAlphaHitTest : MonoBehaviour
    {
        [SerializeField, Range(0.01f, 1f)] float _threshold = 0.1f;

        void Awake() => Apply(GetComponent<Image>(), _threshold);

        public static void Apply(Image image, float threshold)
        {
            if (image == null || image.sprite == null) return;
            if (!image.sprite.texture.isReadable)
            {
                Debug.LogWarning($"Tent: '{image.sprite.name}' is not Read/Write enabled, so its whole rectangle takes clicks.", image);
                return;
            }
            image.alphaHitTestMinimumThreshold = threshold;
        }
    }
}
