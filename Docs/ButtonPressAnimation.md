# ButtonPressAnimation

uGUI の `Button` に付けて使う DOTween 製の押下アニメーションです。ゲーム進行・シーン遷移・効果音には依存しません。既存の Button と共存し、クリックの処理は `Button.onClick` がそのまま担当します。

## 適用済みの範囲

- `PauseDialog.prefab` / `SettingsDialog.prefab`：各 4 ボタン。
- Title / StageSelect / Shop / Main / TestScene/OutGameTest の個別ボタン：計 9 件。
- Prefab からの配置を含めて 5 シーン・計 33 ボタン。非アクティブなダイアログ内も適用済みです。

## 新しいボタンで使う

1. Button の GameObject に、Add Component → UI → Effects → Button Press Animation を追加します。
2. 必要なら Prefab に保存します。実行中に Instantiate するボタンにも引き継がれます。
3. Inspector で倍率、時間、Ease を調整します。onClick への手動登録は不要です。

`Animation Target` が未設定なら Button 全体を拡縮します。判定領域を動かしたくない場合は、親 Button に固定サイズの Raycast 用 Graphic を置き、背景・文字をまとめた子を Animation Target に指定してください。子の表示用 Graphic の Raycast Target は無効にします。親の子として配置した見た目だけが動き、LayoutGroup などの配置と分離できます。

`Hover Scale` はホバー／選択時、`Pressed Scale` は押下中の XY 倍率、`Click Overshoot Scale` は決定時の跳ね返り倍率です。元のスケールを基準にし、Z は変更しません。`Animate Selection` でキーボード／ゲームパッド選択時の拡大を切り替えられます。`Use Unscaled Time` は初期値が有効で、ポーズ中にも動きます。

同じ Target のスケールを別の Animator／Tween と同時に操作しないでください。再利用時の元サイズは OnEnable で取得するので、サイズ変更は無効化中に行ってから再表示します。

## 入力と後始末

押下中に範囲外へ移動すると見た目が戻り、指を離さず戻ると再び沈み込みます。別の指を離しても元の押下は解除しません。Button の無効化、interactable／親 CanvasGroup の変更、コンポーネント無効化、フォーカス喪失で Tween を停止し、元の大きさへ戻します。

Tween はこのコンポーネントが作ったものだけを停止します。完了・Kill 時に参照を破棄するため、DOTween のリサイクル設定が有効でも別の Tween を誤って停止しません。

`PlayClickFeedback()` は演出のみを再生する公開メソッドです。通常は Button.onClick から自動実行され、マウス／タッチ／Submit を扱います。ボタンの処理を遅延しません。クリック直後に閉じるダイアログや遷移する画面では、その時点で演出も終了します。

## レビューで修正した点と検証

実行時の全シーン走査を廃止し、シーン／Prefab に明示的に保存しました。これにより Inspector で編集でき、実行中に生成した Prefab にも適用されます。Button.enabled の判定、再表示時の選択・基準サイズ復元、複数ポインターの所有権、Tween 終了時の参照解放を追加しました。

Unity 6000.3.20f1 の Play Mode で一時オブジェクトを作成し、押下・範囲外移動・連打・無効化・CanvasGroup・再表示・フォーカス・選択・Submit・即時クローズなど 18 項目のアサーションを実行して成功しました。追加で timeScale=0 の実時間再生、子 Target のみの変形、実行中に生成した PauseDialog の 4 ボタンへの適用を確認しました。テスト用オブジェクトは破棄し、検証時の時間・バックグラウンド実行設定は復元しています。
