using UnityEngine;

public class PlayerSpawnPoint : MonoBehaviour
{
    [SerializeField] private bool useOnSceneLoad = true;
    [SerializeField] private int priority;

    public bool UseOnSceneLoad => useOnSceneLoad;
    public int Priority => priority;
}
