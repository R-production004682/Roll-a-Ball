using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    public sealed class StageSelectController : MonoBehaviour
    {
        private void Awake() => OutGameStateController.Enter(GameFlowState.Menu);
    }
}
