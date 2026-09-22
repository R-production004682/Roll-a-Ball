# FX-S2-WIND-01

ステージ2「迷いの密集林」に配置する、葉擦れと湿った光粒の風エフェクトです。

## できること

- 葉片が同じ方向へ流れ、途中で揺らぎながら循環します
- 少量のシアン系の湿った光粒が風の流れを補助します
- `WindSwayTarget` を付けた木や草が、同じ風向き・強さ・乱流で揺れます
- `Stage2WindEffect` の Inspector または公開 API から風を調整できます
- 粒子数に上限を設け、局所範囲の外へ出た粒子を反対側へ循環させます

## 配置方法

1. `Prefab/FX-S2-WIND-01_wind_flow.prefab` を木の集まりの近くへ配置します
2. 木や草の「見た目だけを動かしたい子オブジェクト」に `WindSwayTarget` を付けます
3. 風エフェクトの子階層に対象を置く場合は、自動で対象を取得します
4. 別階層の対象を動かす場合は、`Stage2WindEffect` の `Sway Targets` に登録します
5. `Wind Direction`、`Wind Strength`、`Turbulence`、`Local Volume Size` を配置場所に合わせて調整します

物理用の親オブジェクトや Collider を直接揺らさず、Renderer を持つ子オブジェクトへ `WindSwayTarget` を付けてください。プレイヤーの移動・トゲ判定・ジャンプ判定には影響しません。

## 公開 API

```csharp
var wind = GetComponent<Stage2WindEffect>();
wind.SetWind(new Vector3(1f, 0.16f, 0.32f), 0.92f);
wind.SetTurbulence(0.48f);
wind.SetFlowRange(new Vector3(6.8f, 3.4f, 6.8f));
wind.Play();
wind.Stop();
```

## テスト

`Assets/Scenes/TestScene/EffectTestScene.unity` を開き、左側の「ステージ2風 ON / OFF」ボタンを押してください。
テスト表示は、木の集まり・草・木漏れ日環境・葉片・湿った光粒をまとめて表示します。
