# タイトルの Any Press Key 確認（2026-10-10）

Unity 6000.3.20f1 の接続済み Editor で確認。
直前に Prefab 化した TitleScreen を変更し、遷移先の StageSelectScene を引き継ぐ。

## 配置・表示

- `Assets/Prefabs/UI/Screens/TitleScreen.prefab` の旧開始ボタンを、表示専用の `AnyPressKey` とその子 TMP `Label` に置き換えた。
- Button / SceneTransitionButton / ButtonPressAnimation / ButtonSoundEffect / ボタン背景 Image を除去。UiInputScope.firstSelected を解除。
- TitleScreen.promptGroup は AnyPressKey の CanvasGroup を参照。destination の Scene パス / GUID は既存ボタンの設定を移行。
- AnyPressKey は中央アンカー、中央から下へ160px、650×100。表示文言は指定どおり `Any Press Key`、フォントサイズ52、文字間隔6。文字の Raycast Target とグループの Blocks Raycasts / Interactable は無効。
- `Assets/Prefabs/UI/Common/AnyPressKeyWarmGlow.mat` は既存の NotoSansJP-Medium SDF マテリアルを複製したもの。クリーム色の Face、暖色の Glow / Underlay、柔らかい輪郭を設定。元のフォントマテリアルはそのまま使用できる。
- TitleScreen の blinkDuration は4、minimumAlpha は0.2。独立時間の InOutSine / Yoyo による反復。

## 実施結果

- Unity の変更後スクリプトコンパイルと Prefab インポートを確認。対象 Prefab の Missing Script は0。
- 再生中の TitleScreen に Button と SceneTransitionButton は0。表示文言・CanvasGroup・遷移先の参照が一致。
- 明滅の0 / 1 / 2 / 3 / 4秒地点で alpha は 1 / 0.6 / 0.2 / 0.6 / 1。timeScale = 0 でも Tween の経過時間が進むことを確認。
- OnClose で Tween が停止・解除され、alpha が1へ戻る。再度 OnOpen すると新しい Tween が1つ開始し、前の Tween は停止したまま。
- Game View のフォーカスを Editor API で有効にし、Input System の KeyboardState（Enter）を送信。TitleScene → StageSelectScene のフェード遷移を確認。
- 一時的な Gamepad に South ボタンの押下状態を送信し、同じ遷移を確認。
- マウス左ボタンの押下状態を送信し、TitleScene → StageSelectScene の遷移を確認。Keyboard / Mouse / Gamepad の新規押下を開始入力として扱う。
- マウス移動とホイールの状態だけを送信した場合は TitleScene に留まる。
- TMP の文字のはみ出しなし。実際の Game View の画像で暖色のぼかしを確認。
- SceneReferenceBuildProcessor.OnPreprocessBuild を実行し、メモリ上で古いパスにした TitleScreen の destination が GUID から StageSelectScene の現パスに同期されることを確認。Prefab の保存値も一致。
- 正常系の再生・入力確認後と最終コンパイル後の Console Error / Warning は0。
- 試験用 Input System デバイス、timeScale、バックグラウンド実行設定を復元。退避した `RollABall.GameData.SaveData` はキーの有無と文字列の一致まで確認して復元。TitleScene を Edit Mode・未保存変更なしで残す。

## 未実施

- ビルドした Player と実キーボード・実マウス・実ゲームパッドによる入力確認。
- 実アセットの Scene 移動・改名を伴うビルド確認。パス同期処理単体は Editor 上で確認済み。
