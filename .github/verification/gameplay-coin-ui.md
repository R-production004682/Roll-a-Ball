# ゲーム中のコイン UI 確認（2026-10-10）

Unity 6000.3.20f1 の接続済み Editor で確認。
Main はこのプレイの獲得金額、StageSelect は保存済みの累計所持金を表示する。
見た目を共通にし、表示するデータと Prefab を分ける。

## 配置・参照

| 対象 | 設定 |
|---|---|
| MainScene / UI / GameplayHud | `Assets/Prefabs/UI/Hud/GameplayHud.prefab` のインスタンス |
| GameplayHud / GameplayCoinDisplay | `Assets/Prefabs/UI/Hud/GameplayCoinDisplay.prefab` をネスト。StageCoinDisplay が GameManager.CollectedCoins を表示 |
| GameplayCoinDisplay RectTransform | anchorMin / anchorMax / pivot = (0, 1)、anchoredPosition = (32, -32)、sizeDelta = (300, 55) |
| GameplayHud Canvas | Screen Space Overlay、sortingOrder = -10 |
| GameplayHud CanvasScaler | Scale With Screen Size、referenceResolution = (1920, 1080)、幅基準 |
| CoinBackground | 左上 (20, -24)、sizeDelta = (324, 71)、半透明の暗色、Raycast Target 無効 |
| StageSelectScreen.currencyLabel | `Assets/Prefabs/UI/Common/StageSelectCoinDisplay.prefab` の TMP を参照。CurrencyDisplay が GameDataManager.Currency を表示 |

StageSelectCoinDisplay は旧 CurrencyDisplay.prefab の GUID を維持して移動したもの。
GameplayCoinDisplay は見た目を複製した独立 Prefab。Main の所持金表示コンポーネントは0。
GameManager の DefaultExecutionOrder(-1000) により、表示の OnEnable より先に共有参照を設定する。
獲得金額は GameManager の非保存プロパティに保持し、シーンの再読込で新しいインスタンスの0から開始する。
取得時の累計所持金への保存は引き継ぎ、保存成功時にだけ獲得金額も更新する。

## 実施結果

- スクリプトコンパイルと Prefab インポート後、Console Error / Warning は0。
- 所持金800の状態で MainScene を開始し、獲得金額0・表示 `COIN  0`・Player 遮断なしを確認。
- 実在するコインに Player Collider の Trigger 通知を送信。設定金額50が獲得金額と表示へ反映され、累計所持金は850となった。同じコインに連続で2回通知しても獲得金額は50のまま。
- GameManager.TryCollectCoin の0・負数・加算時の int オーバーフロー候補は false。獲得金額と所持金は変更なし。
- SceneRouter 経由で MainScene を再読込し、累計所持金850を維持したまま獲得金額0・表示 `COIN  0` へ戻ることを確認。
- StageSelectScene に戻ると `COIN  850` を表示し、Main 用の StageCoinDisplay は0。両方の TMP の font / fontSharedMaterial / color / fontSize が一致。
- StageSelectScene から MainScene へ戻った場合も `COIN  0`。10を取得後、ポーズ・再開の両方で表示 `COIN  10` を維持。
- StageCoinDisplay を無効化・再有効化しても取得金額を維持。購読数は1 → 0 → 1で重複なし。
- 実際の Game View 画像で左上の `COIN 0` を確認。
- 試験前の `RollABall.GameData.SaveData` を退避し、終了時にキーの有無と文字列の一致まで確認して復元。バックグラウンド実行設定を復元し、MainScene を Edit Mode・未保存変更なしで残した。

## 未実施

- ビルドした Player、実操作による物理接触、保存失敗時の実ストレージ障害試験。
- TestScene 配下の検証用 UI の Prefab 化。以前の UI 移行範囲は TitleScene / StageSelectScene / MainScene。
