using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// ポーズ状態を確認するためのテストオブジェクトを制御
    /// </summary>
    public sealed class PauseTestObjectController : MonoBehaviour
    {
        [SerializeField] private float moveRange = 2f;
        [SerializeField] private float moveSpeed = 1.5f;
        [SerializeField] private float rotationSpeed = 120f;

        private Vector3 initialPosition;

        private void Awake()
        {
            initialPosition = transform.position;
            CreateTestSphere();
        }

        private void Update()
        {
            var moveOffset = Mathf.Sin(Time.time * moveSpeed) * moveRange;

            transform.position = initialPosition + Vector3.right * moveOffset;
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        private void CreateTestSphere()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

            sphere.name = "PauseTestSphere";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localScale = Vector3.one * 1.5f;
        }
    }
}
