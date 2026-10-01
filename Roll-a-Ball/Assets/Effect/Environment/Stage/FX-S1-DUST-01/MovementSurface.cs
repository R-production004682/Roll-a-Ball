using UnityEngine;

/// <summary>
/// 地面のColliderまたはその親に付けて移動粉塵の種類を指定する
/// </summary>
[DisallowMultipleComponent]
public sealed class MovementSurface : MonoBehaviour
{
    [Header("この地面のエフェクト設定")]
    [Tooltip("この地面に使うMovementSurfaceProfile。色・粒子数・草片/小石をProfileで調整します。同じProfileを共有する地面すべてに変更が反映され、未設定なら粉塵を出しません。")]
    [SerializeField] private MovementSurfaceProfile profile;

    public MovementSurfaceProfile Profile => profile;
}
