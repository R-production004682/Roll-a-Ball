# FX-S1-DUST-01 草地・土の移動エフェクト

## 役割

Playerが地面に接したまま移動すると、移動方向の後ろへ、地面に設定した微細な粉塵を出す。Grassでは短い葉身をした草片、Dirtでは小石を少量追加する。停止・ジャンプ・落下・未設定の地面では新しい粒子を出さない。

Issue #48 は地面名を `Dart` と記載しているが、関連する #136 の `Dirt` に合わせ、土のProfile名は `DirtMovementSurface` としている。両方ともテスト表示で分かるようにしている。

## PrefabとAPI

- 再利用Prefab: `Prefab/FX-S1-DUST-01_ground_dust.prefab`
- 粉塵用Shuriken: `Dust`（24枚まで、輪郭と密度にむらのある64pxアルファマスク）
- 少量の色付き光粒: `Sparkles`（2枚まで。草・土の主役にはしない）
- 草片: `SurfaceDetails`（8枚まで）
- 小石: `Pebbles`（6個まで）。草とは別Systemなので、地面を切り替えても既存の草が小石に変形しない
- 地面設定: `Profile/GrassMovementSurface.asset`、`Profile/DirtMovementSurface.asset`
- 草の折り目と先細りの葉身メッシュ: `Mesh/MovementGrassBlade.asset`（16三角形）
- 土の小石メッシュ: `Mesh/FX-S1-DUST-01_faceted_clod.fbx`（厚みのある80三角形、幅1mに正規化）
- 粉塵Material: `Material/MovementDust.mat`（URP Particles/Unlit、Alpha Blend、非発光）
- 草・石Material: `Material/MovementDebris.mat`（URP Particles/Lit、Alpha Blend、非発光）
- 移動の観測: `PlayerMovementEffect`
- 粒子の再生API: `GroundDustEffect.Play(surface, velocity, point, normal, grounded, deltaTime)`
- 停止API: `GroundDustEffect.Stop()`。リスポーン、ワープ、無効化では `Stop(true)` / `PlayerMovementEffect.ResetEffect()` を呼び、残留粒子も消す

Profileでは、粉塵と表面粒子それぞれの色・速度ごとの放出数・大きさ・寿命を調整できる。草片・小石の種類、横方向の広がり、跳ね上がり、重力もProfileごとに設定する。土は重力で早く落ちる小粒の小石、草は短い先細りの葉身が低く散る表現。

Meshは1m基準。実際の草片は14～24cm、小石は6～11cm。粉塵は35～55/100秒で消える。草・小石は地面へ戻る飛行時間でも寿命を制限する。粒子上限は合計40個。

2026-09-23の見直しでは、粉塵Meshが実幅100mだったFBXスケールを修正し、粉塵を専用マスクのBillboardへ、小石を立体FBXへ分離した。草片の角度指定もEmitParamsの度単位に修正した。Blenderの小石編集元は `SourceArt/GroundDebris/GroundDebris.blend`。

## Player・地面への設定

`PlayerMovementEffect` は次のフィールドを設定したうえで Player に付ける。

- `Player Collider`: Playerの当たり判定。自身と子Colliderは地面として除外する
- `Effect`: 上記Prefabのインスタンスにある `GroundDustEffect`
- `Ground Layers`: 地面Colliderのレイヤー（初期値は全レイヤー）

地面のCollider、またはその親に `MovementSurface` を追加し、`Profile` に Grass または Dirt を指定する。地面設定のないColliderからは粒子を出さないため、空中・壁際・未対応地面で意図せず粉塵が出ない。

MainSceneの基礎Planeは青白いテスト模様で草地・土のいずれとも判断できないため、種類を決め打ちしない。森林ステージの実際の草地Colliderと土道Colliderに `MovementSurface` を付けて使う。

## EffectTestSceneでの確認

1. `Assets/Scenes/TestScene/EffectTestScene.unity` を開く
2. Playし、既存の「移動粉塵（草 / 土）」をクリックする
3. 展示用Playerが自動で動き、低速・停止・草地から土・ジャンプと落下・逆向き移動を巡回する
4. 草地は粉塵と短い草片、土は粉塵と小石に切り替わることを確認する。停止中・ジャンプ中・落下中には放出せず、停止・リスポーンで粒子が残らない
5. Profile値を調整すると、速度・色・粒径・寿命・追加の草片/小石の量が更新される

展示専用Ballと地面はテストシーン内だけに置き、本番Playerの入力や物理設定は変更しない。表面種は `MovementSurface` とProfileだけで切り替わるので、将来は苔や木材など別Profileを追加できる。

## 修正後の検証

Unity 6000.3.20f1 Play Modeで `MovementDustValidation.Validate()` を実行しPASS。停止・離陸・落下・ワープ・リスポーン・10回再利用・複数同時再生・地面切替での草Mesh維持・粒子残留を検証した。低速時7個、高速時18個、上限40個。ウォームアップ後の移動処理120回でGC割当0バイト。

実粒子を進めて停止した状態をUnityカメラで撮影し、近接と既存ゲームカメラで目視確認。画像は `Roll-a-Ball/Captures/VfxRepair/grass_after.png`、`dirt_after.png`、`dust_quarter_after.png`。モバイル実機とGPU負荷測定は未実施。
