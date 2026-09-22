# Stage 1 エフェクト一覧

Prefabは各エフェクトIDのフォルダ直下に置いています。使いたいPrefabをそのままHierarchyへドラッグしてください。

- `FX-S1-LIGHT-01/FX-S1-LIGHT-01_dappled_light.prefab` — 木漏れ日
- `FX-S1-LEAF-01/FX-S1-LEAF-01_leaf_particles.prefab` — そよ風の葉・光粒

---

# 木漏れ日 — FX-S1-LIGHT-01

森のステージ向けの、葉の隙間から差す日光です。今回は個別の依頼に合わせ、共通の青いクリスタル演出ではなく、暖かい光と自然な散乱を狙っています。

## まず見てみる

1. `Assets/Scenes/TestScene/EffectTestScene.unity` を開き、Playします。
2. 左上の **木漏れ日 ON / OFF** を押します。
3. 地面の明るい模様、斜めに差す光の筋、小さな浮遊粒子を確認します。
4. もう一度押すと、展示を消して元の照明に戻します。

Hierarchyの `DappledLightTestPreview` は、保存時には非アクティブです。ボタンが木漏れ日と比較用の展示をまとめて有効にします。コイン・ゴールの確認へ戻るときは木漏れ日をOFFにしてください。

木・石・苔の床・白い球は照明確認用の簡易モデルです。写実的な森の完成アートではありません。テスト中だけ環境光、Directional Lightの明るさ、カメラ背景、ポストプロセスを切り替え、終了時に復元します。他のシーンの設定は変更しません。

## ステージに置く

1. `Assets/Effect/Stage1/FX-S1-LIGHT-01/FX-S1-LIGHT-01_dappled_light.prefab` をHierarchyへドラッグします。
2. Prefabの原点を、日差しを当てたい地面の位置に合わせます。最初はScaleを `(1, 1, 1)` にしてください。
3. Prefab全体をY軸で回すと、日差しの方向が変わります。カメラの正面・背面の両方から確認してください。

置いて有効にするだけで連続再生します。接触判定やインゲームコードへの追記は不要です。

構成:

```text
FX-S1-LIGHT-01_dappled_light
├─ DappledLightEffect       ライトと散乱を同期
├─ CanopySun               Cookie付きSpot Light／リアルタイムの影
├─ ScatteringVolume        光が散乱する範囲（立方体メッシュ）
└─ SunlitMotes              低密度の浮遊粒子
```

コードで切り替える場合は、Inspectorで参照を持ち、`effect.SetVisible(true)` / `effect.SetVisible(false)` を呼びます。共有マテリアルの値は書き換えません。Scriptだけを無効にすると散乱の更新が停止します。ライトと粒子を含めて止める場合はPrefab全体を切り替えてください。

### 必要な描画設定

- Unity 6.3 / URP 17.3。現プロジェクトのPC設定（Forward+）で確認しています。
- URPの **Depth Texture**、**Additional Lights**、**Light Cookies**、**Additional Light Shadows** を有効にします。現PC設定は既に有効なので、今回は共通Rendererを変更していません。
- 照らされる床・ボールはURP/Litなど、ライトを受けるマテリアルが必要です。Unlitは明るくなりません。
- 光を遮る物体のRendererは **Cast Shadows** を有効にします。
- Bloomを使う場合はHDRとCameraのPost Processingを有効にします。Bloomなしでも光の筋は表示できます。
- `DappledLightTestProfile.asset` はテスト専用です。本番ステージへ自動適用しません。既存のVolumeで弱いBloomと露出を調整してください。

## 見た目の調整

| 変えたいこと | 操作 |
|---|---|
| 地面を明るく／暗くしたい | `CanopySun > Light > Intensity`。初期値1400は、光源が地上約22mにある配置の値です |
| 光の筋だけを濃く／薄くしたい | `DappledLightEffect > Scattering`。初期値0.16。まずここを少しずつ変更します |
| 空気の濃さを変えたい | `Density`。初期値0.035。上げすぎると霧っぽくなります |
| 光を見る向きによる強弱を変えたい | `Anisotropy`。初期値0.2。上げるほど逆光方向で強くなります |
| 木漏れ日の揺れを止めたい | `Foliage Sway` を0。初期値0.25度。Cookieと実ライトを同時にゆっくり回しています |
| 葉の隙間の模様を変えたい | `CanopySun > Cookie` の画像を交換。白が光を通し、黒が遮ります |
| 光の筋の範囲を変えたい | `ScatteringVolume` の位置／Scale。初期範囲は12×9×12m。端は滑らかに薄くなります |
| 床の照射範囲を変えたい | `CanopySun` の向き・Spot Angle・Range。散乱範囲とは別です |
| 粒子を減らしたい | `SunlitMotes > Emission > Rate over Time`。初期値6、最大100粒子 |
| 負荷を下げたい | `Sample Count` を96から64、必要なら48へ。細い筋の粒状感が増えるため実画面で比較します |

Prefab全体の拡大だけではLightのRangeや粒子の設定を含む全体の見え方は一致しません。大きさを変更する場合は上記の各範囲を調整してください。

## 方式を選んだ理由

地面の光はURP標準のSpot Light + Cookieで作り、空中の光は専用Shaderで計算しています。Shaderはカメラから見える局所的な空間を細かくサンプリングし、同じライトのCookie・減衰・リアルタイム影を参照します。地面や不透明な物体の深度で積分を止めるので、その奥にある光が物体の手前へ透けて出ることを防ぎます。

板状の光線画像をカメラに向けて配置する方式ではありません。透視投影・正投影の両方に対応し、向きを変えても立体的な光の筋として表示します。上端と側面は徐々に薄くなります。

局所的な単一散乱を加算する、リアルタイム向けの近似表現です。画面全体の物理的な霧、多重散乱、間接光まで再現する大気シミュレーションではありません。世界全体を一様に暗くせず、ゲームの見やすさを保つ設計です。

### 参照したUnity公式情報

- [URPのLight設定](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/light-component.html)：Cookie、Spot Light、リアルタイム影の設定。
- [深度からワールド座標を復元する方法](https://docs.unity3d.com/6000.3/Documentation/Manual/urp/writing-shaders-urp-reconstruct-world-position.html)：遮蔽物までの描画距離と、プラットフォーム間の深度値の扱い。
- [レンダーパイプラインの機能比較](https://docs.unity3d.com/6000.3/Documentation/Manual/render-pipelines-feature-comparison.html)：URPとHDRPの機能差。HDRPへの変更や外部の有料アセット導入は行っていません。
- [Editorの非同期Shaderコンパイル](https://docs.unity3d.com/ja/2020.1/Manual/AsynchronousShaderCompilation.html)：未コンパイル時のシアンの代替表示を避けるため、このShaderだけEditor同期コンパイルを指定。初回には短い待ちが発生することがあります。
- このプロジェクトのURP 17.3同梱ソース `RealtimeLights.hlsl`、`Shadows.hlsl`、`LightCookie.hlsl`、`ForwardLights.cs` で、Forward+のライト番号、Cookie Atlas、影のサンプリングを照合しました。パッケージ本体は変更していません。

上記資料がこの専用Shader全体を推奨しているという意味ではなく、公開された仕組みを元にこのゲーム用に構成しています。

## 品質と負荷の注意点

- 主な負荷は「画面内の散乱範囲の面積 × Sample Count × 重なったPrefab数」とリアルタイム影です。ステージ全体を多数のVolumeで重ねて埋めず、見せたい場所へ絞って置いてください。
- 範囲内でもCookieが暗い箇所は影のサンプリングを省略します。描画範囲外のVolumeは通常のカリング対象です。
- クラシックForwardでは同時追加ライト数の上限により対象ライトが外れる可能性があります。現プロジェクトのForward+を推奨します。
- ライトを位置で照合するため、別ライトを完全に同じ位置に重ねないでください。Prefab複製時は位置を分けてください。
- 標準の深度を書かない透明オブジェクトは光の筋を遮りません。ガラスの色付き透過・屈折、XR、モバイル、Web、反射用カメラ、実機ビルドは未検証です。
- HDR/Bloomだけでは光の筋は生まれません。逆にBloomを強くしすぎると木漏れ日の模様が白くつぶれます。
- 最終的な写実感はステージの樹木・地面の質感、環境光、カメラと太陽の向きにも依存します。商用ゲーム相当の画質やFPSを保証するものではありません。ターゲット実機のGPU Profilerで確認し、必要ならSample Countを落として調整してください。

## 確認記録

Unity 6000.3.20f1 / URP 17.3、Windows Editorで実施:

- C#コンパイル、専用Shaderのエラーなし。
- EffectTestSceneのボタンで表示／非表示。
- クォータービューの透視投影・正投影・逆側からの表示。
- テスト用遮蔽物の影と、ライトOFF時に光の筋も消えること。
- ON/OFFを10回繰り返し、環境光・Directional Light・Post Processingが復元されること。

未実施: 実機ビルドとGPU負荷計測、ゲーム本番のステージ全体での最終ルック確認。

`Editor/DappledLightAssetBuilder.cs` は初期アセット作成用です。通常の利用で実行する必要はありません。作成済みPrefabのパラメータを再生成で上書きせず、Inspectorから調整してください。

---

# そよ風の葉・光粒 — FX-S1-LEAF-01

木漏れ日の林道で、葉と淡い光粒が風に運ばれていく環境エフェクトです。画面全体を飾るのではなく、プレイヤーが「森に風が通っている」と感じられる密度にしています。

## まず見てみる

1. `Assets/Scenes/TestScene/EffectTestScene.unity` を開き、Playします。
2. 必要なら先に **木漏れ日 ON / OFF** を押して、木漏れ日の展示を表示します。
3. **そよ風の葉 ON / OFF** を押します。
4. 葉がゆっくり不規則に回転しながら流れ、淡い光粒が少し遅れて同じ風に乗ることを確認します。
5. もう一度押すと展示が消えます。長時間再生しても同じ範囲を循環します。

`LeafWindTestPreview` は保存時には非アクティブです。テスト用の木・地面・木漏れ日を一緒に表示するための展示で、本番ステージへ自動追加されません。`WindDemoTree` と `WindDemoGrass` は、風による揺れを確認するためだけの簡易モデルです。

## ステージに置く

1. `Assets/Effect/Stage1/FX-S1-LEAF-01/FX-S1-LEAF-01_leaf_particles.prefab` をHierarchyへドラッグします。
2. Prefabの原点を、木漏れ日を置いた林道の中心付近へ合わせます。
3. 有効にするだけで常時再生します。接触判定やインゲームコードへの追記は不要です。

構成:

```text
FX-S1-LEAF-01_leaf_particles
├─ LeafWindEffect        風向き・範囲循環・再生を制御
├─ Leaves                低ポリの葉を不規則に回転させて運ぶ
├─ LightMotes            葉の動きを補助する淡い光粒
└─ WindZone              対応した植生モデルへ風を渡すDirectional Wind Zone
```

葉は画像の板ではなく、折れを持つ小さな低ポリメッシュです。粒子の寿命でフェードしながら、風向き側の端へ出た粒子は反対側へ循環します。そのため、一定時間ごとに全粒子が同じ場所から出てくる見え方になりません。

コードから制御する場合は、Prefabの `LeafWindEffect` を参照して `Play()`、`Stop()`、`SetVisible(bool)` を呼びます。ステージ固有の処理を `LeafWindEffect` へ追加せず、風向きだけ変える場合は `SetWind(direction, speed)` を使います。

## Inspectorで調整する値

| 変えたいこと | 操作 |
|---|---|
| 風が進む向き | `Wind Direction`。XZ平面を中心に指定します。Yを少しだけ正にすると自然に浮きます |
| 葉が流れる速さ | `Wind Speed`。初期値は0.34。上げすぎるとそよ風ではなく強風に見えます |
| 動きの不規則さ | `Turbulence`。初期値は0.16。葉の軌道を少しだけ揺らします |
| 風が届く範囲 | `Local Volume Size`。葉と光粒の発生・循環範囲です |
| 木や草へ渡す風 | 子オブジェクト `WindZone` の `Wind Main`、`Turbulence`。SpeedTreeやWind Zone対応の植生モデルで使用します |
| 葉の密度 | `Leaves > Emission > Rate over Time` と `Max Particles` |
| 光粒の密度 | `LightMotes > Emission > Rate over Time` と `Max Particles` |
| 葉を大きく／小さく | `Leaves > Main > Start Size`。クォータービューで見えない場合だけ少し上げます |

初期値は葉42枚、光粒72個、葉の寿命8〜15秒です。葉と光粒を合わせても、コイン取得やクリアセレブレーションのような一時的な演出より目立たないように設定しています。

## トンマナと見え方の注意

- 緑〜黄緑を基本にし、光粒だけ淡いシアン寄りにしています。
- クリスタル感は縁の淡い発光だけに留め、ネオン色や画面全体を覆う光にはしていません。
- リングや円形の衝撃波は使わず、葉の面の回転と粒子の流れで風を見せます。
- クォータービューで見えるサイズを確保しつつ、プレイヤーの操作視認性を優先します。
- 既存の木漏れ日Prefabとは独立しています。木漏れ日をOFFにして葉だけを確認することもできます。
- `WindZone` はSpeedTreeや対応した植生Shaderへ風を渡す共通設定です。通常のURP/LitのCubeやSphereはWind Zoneだけでは変形しません。
- EffectTestSceneの簡易樹冠は、Wind Zoneの値を使うテスト用コンポーネントで揺れを見えるようにしています。本番の木・草モデルへ同じコンポーネントを持ち込む必要はありません。

## 確認記録

Unity 6000.3.20f1 / URP 17.3、Windows Editorで確認:

- そよ風の葉テストボタンで表示／非表示。
- 木漏れ日展示と同時に表示して、葉・光粒が日差しの空気感を邪魔しないこと。
- クォータービューから見た葉の立体感、不規則な回転、風向きの連続性。
- EffectTestSceneの樹冠がWind Zoneの強さに合わせてゆっくり揺れること。
- ON/OFFと長時間再生で粒子が枯れず、画面外へ消え続けないこと。
- 既存の木漏れ日、コイン、ゴールのPrefabとコードを変更していないこと。

未実施: 実機ビルド、ステージ1本番地形での最終ルック確認、GPU負荷計測。

`Editor/LeafWindAssetBuilder.cs` は初期アセットとテスト展示の生成用です。作成済みPrefabの設定を再生成で上書きせず、Inspectorから調整してください。
