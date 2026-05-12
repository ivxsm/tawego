using UnityEngine;

/// <summary>
/// Narrow trigger in the pipe gap; fires once when the bird passes (counts as one passed pipe).
/// </summary>
public class PipeScoreTrigger : MonoBehaviour
{
    bool _used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used) return;
        if (other.GetComponent<bird_script>() == null) return;
        var logic = LogicScript.Instance;
        if (logic == null || !logic.CanScore()) return;
        _used = true;
        logic.RegisterPipePassed();
        gameObject.SetActive(false);
    }
}
