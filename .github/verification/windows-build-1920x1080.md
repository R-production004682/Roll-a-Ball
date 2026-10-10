# Windows 1920×1080 ビルド（2026-10-10）

- Unity: 6000.3.20f1
- Build target: StandaloneWindows64 / Player
- Development Build: 無効
- 初期解像度: 1920×1080、defaultIsNativeResolution = false
- 全画面モード: 既存の FullScreenWindow を維持
- URP PC_RPAsset の Render Scale: 1
- 出力: `Roll-a-Ball/Builds/Windows/Roll-a-Ball.exe`
- シーン: TitleScene → MainScene → StageSelectScene の登録順。起動シーンは TitleScene
- 削除済みの OutGameTestScene / ShopScene を EditorBuildSettings の Scene List から除外

## 結果

- Unity BuildReport: Succeeded / totalErrors = 0 / totalWarnings = 0
- ビルド所要時間: 約65秒、報告サイズ: 254.06MB
- EXE、UnityPlayer.dll、MonoBleedingEdge、Roll-a-Ball_Data/globalgamemanagers の生成を確認
- 通常権限で -batchmode -nographics による12秒の起動確認。エンジン・Input System・アセンブリ・初期シーンの読み込みが完了し、Error / Exception / Missing Script の記録なし。試験後にプロセスを停止
- 制限環境での最初の起動試験は PlayerPrefs 書き込みを拒否された。通常権限での試験では再発なし
- 起動ログは `Roll-a-Ball/Builds/Verification/startup-check-normal.log`
- Editor の MainScene は Edit Mode・未保存変更なし

## 未実施

- ビルドした Player の実画面による解像度・描画・入力・シーン遷移・コイン取得の確認
- 別の Windows PC での起動
