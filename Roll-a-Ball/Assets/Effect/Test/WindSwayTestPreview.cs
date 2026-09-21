using UnityEngine;

/// <summary>
/// Wind Zoneに対応しない簡易モデルでも木の揺れを確認できるテスト用表示を制御する
/// </summary>
public sealed class WindSwayTestPreview : MonoBehaviour
{
    [SerializeField, Range(0f, 10f)] private float swayAngle = 2.4f;
    [SerializeField, Range(0f, 1f)] private float positionSway = 0.08f;
    [SerializeField, Range(0f, 2f)] private float swaySpeed = 0.42f;

    private Transform[] canopies;
    private Vector3[] restPositions;
    private Quaternion[] restRotations;
    private float[] phases;
    private WindZone windZone;
    private bool initialized;

    /// <summary>
    /// テスト展示内の樹冠を収集して初期姿勢を保存する
    /// </summary>
    private void Awake()
    {
        var renderers = GetComponentsInChildren<Renderer>(true);
        var collected = new System.Collections.Generic.List<Transform>();
        for (var i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].name.StartsWith("Canopy_", System.StringComparison.Ordinal))
            {
                collected.Add(renderers[i].transform);
            }
        }

        canopies = collected.ToArray();
        restPositions = new Vector3[canopies.Length];
        restRotations = new Quaternion[canopies.Length];
        phases = new float[canopies.Length];
        for (var i = 0; i < canopies.Length; i++)
        {
            restPositions[i] = canopies[i].localPosition;
            restRotations[i] = canopies[i].localRotation;
            phases[i] = i * 1.73f;
        }

        windZone = GetComponentInChildren<WindZone>(true);
        initialized = true;
    }

    /// <summary>
    /// Wind Zoneの強さを使って樹冠をゆっくり不規則に揺らす
    /// </summary>
    private void LateUpdate()
    {
        if (!initialized || !Application.isPlaying)
        {
            return;
        }

        var main = windZone == null ? 0.34f : windZone.windMain;
        var turbulence = windZone == null ? 0.16f : windZone.windTurbulence;
        var amplitude = swayAngle * Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(main));
        var speed = swaySpeed + turbulence * 0.35f;
        for (var i = 0; i < canopies.Length; i++)
        {
            var time = Time.time * speed + phases[i];
            var sway = Mathf.Sin(time) * amplitude + Mathf.Sin(time * 0.63f + 0.8f) * amplitude * 0.35f;
            var offset = new Vector3(Mathf.Sin(time * 0.71f) * positionSway * main,
                Mathf.Sin(time * 0.49f + phases[i]) * positionSway * 0.35f,
                Mathf.Cos(time * 0.67f + phases[i]) * positionSway * main);
            canopies[i].localPosition = restPositions[i] + offset;
            canopies[i].localRotation = restRotations[i] * Quaternion.Euler(sway * 0.45f, sway * 0.2f, sway);
        }
    }

    /// <summary>
    /// テスト表示を無効化したときに樹冠を初期姿勢へ戻す
    /// </summary>
    private void OnDisable()
    {
        if (!initialized || canopies == null)
        {
            return;
        }

        for (var i = 0; i < canopies.Length; i++)
        {
            if (canopies[i] != null)
            {
                canopies[i].localPosition = restPositions[i];
                canopies[i].localRotation = restRotations[i];
            }
        }
    }
}
