# FX-S2-FOG-01 — 密集林ローカルフォグ

Issue: https://github.com/R-production004682/Roll-a-Ball/issues/137

低彩度の青灰色で、森の足元と遠景だけをゆっくり霞ませる環境エフェクト。木漏れ日・風のPrefabやRenderSettings.fogは変更しない。

## 配置・有効化

1. `Prefab/FX-S2-FOG-01.prefab` をステージ2のシーンに配置する。他ステージには配置しない。
2. TransformのPositionを範囲の中心、Scaleを範囲の幅・高さ・奥行きにする。初期値は中心 `(0, 3.5, 4)`、大きさ `(24, 9, 32)`。正のScaleを使用し、Static Batching対象にしない。
3. 描画するCameraの **Rendering > Depth TextureをOn** にする。PCのURP Assetでは有効だが、Mobileは無効なのでカメラ側のOverrideが必要。Opaque Textureは不要。
4. `Stage2FogEffect` のInspectorで濃さなどを調整する。GameObjectまたはコンポーネントを無効化すれば霧も止まる。コードからは `SetVisible(bool)`、`SetQuality(Stage2FogEffect.FogQuality)` を呼べる。
5. ステージシーンをUnloadすればフォグも消える。DontDestroyOnLoadにしない。リザルトで背景の霧も不要なら `SetVisible(false)` を呼ぶ。

Screen Space OverlayのUIはフォグ描画後に描かれる。リザルト・デバッグ表示もOverlayまたは別のUI用Overlay Cameraを使用する。ワールド内の半透明素材は別途ソート順の確認が必要。

## Inspector

| 項目 | 初期値 | 役割 |
| --- | --- | --- |
| Fog Color | 青灰 `(0.38, 0.46, 0.49)` | 霧の色。Alphaは使わず濃さで調整 |
| Density | 0.10 | 1mあたりの減衰強度。まず0.06〜0.12で調整 |
| Start Distance | 12m | カメラ近接面から霧が増え始める距離 |
| End Distance | 36m | 距離による強度が最大になる距離。範囲の終端ではない |
| Height Falloff | 2 | 大きいほど箱の下側に霧を寄せる。高さは箱内の相対値 |
| Noise Scale | 0.3 | ワールド空間のノイズの細かさ |
| Noise Speed | `(0.055, 0.005, 0.025)` m/s | ゆっくり移流する方向・速度 |
| Noise Strength | 0.65 | 濃淡の揺らぎ |
| Maximum Opacity | 0.45 | 1ボリュームで元の色を最低55%残す上限 |
| Quality | Medium | Low=8 / Medium=16 / High=24サンプル |

足場が白くなりすぎるときはDensityを下げるかStart Distanceを伸ばす。カメラの高さ・ズームに合わせて距離も調整する。複数の箱を重ねるとOpacity上限は合算されるため、基本は1つを使う。

## 構成・描画予算

- Environmentのみ、常時ループ。光粒・歪み・追加ライト・音・カメラ揺れは不採用。
- 1 Cube / 12三角形 / 1 Material / 1描画パス。追加テクスチャ・粒子は0。
- HLSLで深度復元 → ローカル箱との交差 → 8〜24点の積分 → Alpha Blend。箱の縁・上側・近距離を滑らかに減衰する。
- Shader Graphの単純な透明面では箱内の視線積分と深度による切り詰めが成立しないため、既存木漏れ日Shaderの深度復元方式に沿った専用HLSLを採用。
- URP / Transparent-10 / Cull Front / ZWrite Off / ZTest Always / SrcAlpha OneMinusSrcAlpha。Opaqueの深度まで積分し、通常の透明VFXやUIより先に描画する。
- 時間乱数や点滅は使わない。1オクターブの連続3Dノイズを移動する。フレームごとのC#更新、Instantiate、Find、GC割り当てはない。

## 軽量化と制約

1. モバイルはLowから開始。見た目の差が出る長い視線のみMediumにする。
2. 箱を必要な森林に絞り、画面占有率と重複数を減らす。
3. URP Render Scaleを下げる場合はゲーム全体への影響を別途評価する。
4. Depth Textureを使えない環境ではフォグ自体を無効化する。深度なしの表示はサポートしない。

前方・後方の箱面がCameraのFar Clipより遠くなる巨大な箱は避ける。Far Clip内に箱全体を収める。透明オブジェクトは通常深度を書かないので、ガラスや半透明の水面を不透明物と同じように霞ませる用途は対象外。XRは未検証。

## 検証環境

`Assets/Scenes/TestScene/Stage2FogTestScene.unity` を開きPlayする。専用の簡易森林、白いボール、欠けた橋、手前と遠方のトゲ、シアンのジャンプ台がある。これらは視認性の目印であり、移動・ダメージ判定は持たない。

Hierarchyの `FX-S2-FOG-01` を選び、Inspectorの `Stage2FogEffect` のチェックでON/OFFを比較する。Qualityや距離も再生中に変更できる。再生中の変更は停止時に戻る。Main Cameraを回転させ、板状に見えないことを確認する。

このシーンはBuild Settingsへ追加していない。Editorでの目視用。Development Buildで端末測定する際だけテスト用Build Profileへ明示追加し、製品用には含めない。フォグ本体は製品ビルドでも使用できる。ユーザー保存データへの操作はない。

詳細な検証手順: https://github.com/R-production004682/Roll-a-Ball/wiki/Stage2-Fog-Test

### 2026-09-24 確認記録

Unity 6000.3.20f1 / Windows Editor / D3D12 / RTX 5080 / Core Ultra 7 265KF / Game View 1920×1080。各設定45フレーム待機後、180フレームの `Time.unscaledDeltaTime` を集計。Editor内の全体フレーム時間であり、GPU単体時間・製品ビルド性能ではない。

| 設定 | 平均ms | 95%点ms | 平均時間からのFPS | Editor Draw Calls（スナップショット） |
| --- | ---: | ---: | ---: | ---: |
| OFF | 1.357 | 1.555 | 737 | 751 |
| Low | 1.365 | 1.633 | 733 | 753 |
| Medium | 1.258 | 1.403 | 795 | 753 |
| High | 1.287 | 1.485 | 777 | 752 |

順序・Editor処理などの変動が含まれ、HighがLowより速いという意味ではない。小さな差からGPUコストは判定できない。高性能PCで大きな低下がないことを確認した参考測定。Frame Debuggerでは不透明描画の後に `DrawTransparentObjects/RenderLoop.Draw` が1イベント、その後にScreen Space UIが描画される。森林モデル・影・SSAOを含む全体のDraw Callとフォグ単体の1パスを混同しない。

- 再生、Shaderエラーなし、コンポーネントON/OFF連続10回でRendererの残留なしを確認。
- 通常の斜め視点でボール・橋の欠け・トゲ・ジャンプ台を識別。反対側からの平行投影、Low品質でも箱の面が板として見えないことを確認。
- カメラを箱内へ移動しても表示が破綻しないことを確認。既存EffectTestCanvasの一時コピーを用いたGame View撮影で、Overlay UIの文字・ボタンが霞まないことを確認。一時コピーは削除済みで検証シーンには含めない。
- 他のステージ・木漏れ日Prefab・URP設定・EffectTestSceneは保存変更していない。
- モバイル実機、製品ビルドのGPU時間、透明水面との組合せは未検証。対象端末でLowから再計測する。
