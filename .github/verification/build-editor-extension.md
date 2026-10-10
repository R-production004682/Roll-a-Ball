# Windows ビルド Editor 拡張の確認

- 確認日: 2026-10-10
- 環境: Unity 6000.3.20f1 / Windows
- 対象: `WindowsBuildWindow.cs`、`WindowsBuildUtility.cs`、`WindowsBuildWindow.uss`

## 実装内容

- `Tools/Roll-a-Ball/Build Windows` から開く UI Toolkit の EditorWindow
- Windows 64bit / 1920×1080、共通 Scene List の有効なシーンを登録順にビルド
- 出力先選択と記憶、Development Build、ビルド後の起動、出力フォルダー表示
- 再生・コンパイル・インポート・ビルド中とビルド予約中は操作を無効化
- 不正な出力先、シーン未登録、存在しないシーンを事前検証

## 実施した確認

- Unity でコンパイル・インポート成功、3 ファイルの `.meta` 生成を確認
- ウィンドウの生成、シーン一覧、ボタンのサイズと有効状態を UI Toolkit の実要素で確認
- `CreateGUI` を繰り返してもスタイルシートが重複しないことを確認
- 通常設定で Development / AutoRun が無効、各オプション指定時に該当フラグが有効になることを確認
- Assets 出力、相対パス、Builds に似た別名のプロジェクト内フォルダーを拒否
- 空の Scene List と存在しないシーンを拒否し、確認後に元の Scene List を復元
- ビルドボタンのコールバックを呼び出して実ビルドを実施。予約中のボタン無効化と完了後の再有効化を確認
- 最新 BuildReport: `Succeeded`、エラー 0、警告 0、約 2.3 秒（既存ビルドのキャッシュを使用）
- 結果表示に成功と EXE パスが表示され、EXE・UnityPlayer.dll・データフォルダーの存在を確認
- 設定値 1920×1080 / native resolution 無効、通常ビルド、終了後のアクティブシーンに未保存変更がないことを確認
- Console にエラーなし。URP の使用アセット一覧の Warning が 1 件あり、BuildReport の警告は 0 件
- `git diff --check` で空白エラーなし

出力先: `Roll-a-Ball/Builds/Windows/Roll-a-Ball.exe`

対象シーン: TitleScene → MainScene → StageSelectScene（共通 Scene List の順序）

## 未実施の確認

- OS 上のマウス操作によるボタン押下、出力先選択ダイアログ、Explorer 表示
- 「ビルドして起動」による Player 自動起動（AutoRun フラグの生成は確認済み）
- 未保存シーンの保存確認ダイアログでのキャンセル操作、Development Build の実ビルド

今回の実ビルドでは通常ビルドのコールバックを Editor 内から呼び出して確認した
