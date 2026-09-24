using UnityEngine;

/// <summary>
/// 地面のColliderまたはその親に付けて移動粉塵の種類を指定する
/// </summary>
[DisallowMultipleComponent]
public sealed class MovementSurface : MonoBehaviour
{
    [SerializeField] private MovementSurfaceProfile profile;

    public MovementSurfaceProfile Profile => profile;
}
