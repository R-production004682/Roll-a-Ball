using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float mouseSensitivity = 200;//マウス視点操作の感度

    [SerializeField]
    private Transform player;//プレイヤー（インスペクターから指定）

    [SerializeField]
    private float cameraDistance = 7f;//プレイヤーからの距離

    [SerializeField]
    private float cameraHeight = 2.2f;//プレイヤーからの高さ

    private float cameraYaw;//カメラの左右の角度

    private void Update()
    {
        if (GameManager.instance.isStageCompleted == true)
            return;//リザルト出た後は操作不可
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;//マウスの左右移動量を取得
        cameraYaw += mouseX;//カメラの左右の角度を更新
    }

    private void LateUpdate()
    {
        Vector3 offset = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(0f, cameraHeight, -cameraDistance);//カメラの位置を計算
        transform.position = player.position + offset;//プレイヤーの位置を追従
        transform.LookAt(player.position + Vector3.up * cameraHeight);//プレイヤーの方向を見る
    }
}
