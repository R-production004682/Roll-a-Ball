using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Roll_a_Ball.EditorTools
{
    /// <summary>
    /// エフェクト専用Materialの調整項目に日本語の説明と調整先を表示する
    /// </summary>
    public sealed class EffectShaderGUI : ShaderGUI
    {
        private static readonly Dictionary<string, GUIContent> PropertyLabels = new()
        {
            ["_BaseColor"] = new GUIContent("Base Color", "葉・蝶の基本色と透明度（A）。頂点色に乗算します。明るさはEmissionで別に調整します。"),
            ["_EmissionColor"] = new GUIContent("Soft Emission", "追加する発光色（HDR）。Emission Strengthを乗算して基本色へ足します。Bloomの見え方はVolume設定にも依存します。"),
            ["_EmissionStrength"] = new GUIContent("Emission Strength", "発光の強さ（0〜3）。大きいほど明るく、0ならEmission Colorによる発光を足しません。ライトの明るさではありません。"),
            ["_EdgeColor"] = new GUIContent("Crystal Edge Color", "視線に対して斜めになった面へ追加する輪郭光の色。強さはEdge Strengthで調整します。"),
            ["_EdgeStrength"] = new GUIContent("Edge Strength", "輪郭光の強さ（0〜2）。大きいほど角度による明るさの差が増え、0なら輪郭光を足しません。"),
            ["_WingFlap"] = new GUIContent("Butterfly Wing Flap", "羽の開閉の振幅（0〜1）。蝶は1、葉は0を基本とし、大きいほど羽が開閉します。速度はButterflySwarmEffectのFlap Frequencyで調整します。蝶専用UVを持つメッシュ向けです。"),
            ["_FogColor"] = new GUIContent("Fog Color", "霧の色。色のAは濃さに使いません。Stage2FogEffectが付いているRendererではコンポーネントのFog Colorが優先されます。"),
            ["_FogParameters"] = new GUIContent("Fog Parameters", "X=濃さ係数（大きいほど濃い）、Y=濃くなり始める距離、Z=濃さが最大になる距離（ワールド座標、Z>Y）、W=不透明度上限。通常はStage2FogEffectで個別に調整します。"),
            ["_NoiseParameters"] = new GUIContent("Noise Parameters", "X=模様の細かさ（大きいほど細かい）、Y=濃淡のばらつき（0〜1）、Z=上へ薄くなる強さ（0で高さ減衰なし）、W=サンプル数（8〜24、大きいほど滑らか・高負荷）。通常はStage2FogEffectで調整します。"),
            ["_NoiseVelocity"] = new GUIContent("Noise Velocity", "霧模様の流れるワールドXYZ速度（距離/秒）。大きいほど速く、ゼロなら静止します。Wは未使用。通常はStage2FogEffectのNoise Speedで調整します。"),
            ["_SunPositionWS"] = new GUIContent("Sun Position / Enabled", "XYZ=Spot Lightのワールド位置、W=光の筋の表示（0=OFF、1=ON）。DappledLightEffectが自動設定する接続用の値なので、通常はLight参照を調整します。"),
            ["_Medium"] = new GUIContent("Medium Parameters", "X=媒質の濃さ、Y=散乱光の明るさ倍率、Z=方向性（0〜0.8、見る角度で明るさが変化）、W=サンプル数（24〜96、多いほど滑らか・高負荷）。通常はDappledLightEffectで個別に調整します。")
        };

        /// <summary>
        /// Unity標準のMaterial編集処理を使い説明付きの項目と共有設定を描画する
        /// </summary>
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            var material = (Material)materialEditor.target;
            var guidance = material.shader.name switch
            {
                "Roll-a-Ball/Effects/Stage2 Local Fog" => "霧の見た目は、描画オブジェクトのStage2FogEffectで調整します。コンポーネントが個別値を設定するため、Materialの値を変えても反映されない場合があります。範囲はオブジェクトのScaleで調整します。",
                "Roll-a-Ball/Effects/Dappled Light Volume" => "光の筋はDappledLightEffectで、色・明るさ・照射範囲は参照先のSpot Lightで調整します。下の接続用の値はコンポーネントが上書きします。",
                _ => "項目名にマウスを重ねると調整方法を表示します。このMaterialを共有する対象すべてに変更が反映されます。蝶の数・飛行・羽ばたきの速さはButterflySwarmEffectで調整します。"
            };
            EditorGUILayout.HelpBox(guidance, MessageType.Info);
            materialEditor.SetDefaultGUIWidths();
            foreach (var property in properties)
            {
                if ((property.propertyFlags & ShaderPropertyFlags.HideInInspector) != 0)
                {
                    continue;
                }
                var label = PropertyLabels.TryGetValue(property.name, out var description)
                    ? description : new GUIContent(property.displayName);
                materialEditor.ShaderProperty(property, label);
            }
            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }
    }
}
