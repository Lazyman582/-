using UnityEngine;

// Put this on an empty GameObject with a BoxCollider2D (Is Trigger) in the scene.
// When the player enters it, every paralax background in the scene starts following.
public class BackgroundFollowTrigger : MonoBehaviour
{
    private bool _triggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered || !other.CompareTag("Player"))
            return;

        _triggered = true;

        foreach (paralax bg in FindObjectsOfType<paralax>())
        {
            bg.StartFollow();
        }
    }
}
