using UnityEngine;

namespace Roll_a_Ball.OutGame
{
    /// <summary>
    /// 室内の PNG を四辺に描き、窓の開口部を正確な矩形として抜く
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class WindowFrameGraphic : UnityEngine.UI.MaskableGraphic
    {
        [SerializeField] private Texture2D roomTexture;
        [SerializeField] private Rect aperture = new Rect(0.239f, 0.209f, 0.522f, 0.678f);
        public override Texture mainTexture => roomTexture != null ? roomTexture : Texture2D.whiteTexture;

        /// <summary>
        /// 窓の開口部を避けた四辺の画像メッシュを生成する
        /// </summary>
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
        {
            vertices.Clear();
            AddQuad(vertices, new Rect(0, 0, aperture.xMin, 1));
            AddQuad(vertices, new Rect(aperture.xMax, 0, 1 - aperture.xMax, 1));
            AddQuad(vertices, new Rect(aperture.xMin, 0, aperture.width, aperture.yMin));
            AddQuad(vertices, new Rect(aperture.xMin, aperture.yMax, aperture.width, 1 - aperture.yMax));
        }

        /// <summary>
        /// 指定した UV 範囲の画像を矩形としてメッシュへ追加する
        /// </summary>
        private void AddQuad(UnityEngine.UI.VertexHelper vertices, Rect uv)
        {
            var bounds = rectTransform.rect;
            var start = vertices.currentVertCount;
            vertices.AddVert(new Vector3(bounds.xMin + uv.xMin * bounds.width, bounds.yMin + uv.yMin * bounds.height), color, new Vector2(uv.xMin, uv.yMin));
            vertices.AddVert(new Vector3(bounds.xMin + uv.xMin * bounds.width, bounds.yMin + uv.yMax * bounds.height), color, new Vector2(uv.xMin, uv.yMax));
            vertices.AddVert(new Vector3(bounds.xMin + uv.xMax * bounds.width, bounds.yMin + uv.yMax * bounds.height), color, new Vector2(uv.xMax, uv.yMax));
            vertices.AddVert(new Vector3(bounds.xMin + uv.xMax * bounds.width, bounds.yMin + uv.yMin * bounds.height), color, new Vector2(uv.xMax, uv.yMin));
            vertices.AddTriangle(start, start + 1, start + 2);
            vertices.AddTriangle(start + 2, start + 3, start);
        }
    }
}
