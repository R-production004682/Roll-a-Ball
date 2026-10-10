# Windows ビルドツール

Unity Editor の **Tools → Roll-a-Ball → Build Windows** から開きます。
このプロジェクト用の、Windows 64bit・1920×1080 の Player を作成するツールです。

## 基本操作

1. 必要なら「参照…」で出力フォルダーを変更します。標準は `Roll-a-Ball/Builds/Windows/` です。
2. 通常の配布では **Development Build をオフ** にします。
3. **Windows ビルド** を押します。完成後にそのまま遊ぶ場合は **ビルドして起動** を押します。
4. 結果がウィンドウ下部に表示されます。成功後、**出力フォルダーを開く** で `Roll-a-Ball.exe` を確認できます。

同じ出力先へのビルドでは既存の実行ファイルとデータが更新されます。
複数のバージョンを残す場合は、出力フォルダーを分けてください。
出力先と Development Build の選択はプロジェクトごとに記憶します。

配布・別 PC へのコピーには、EXE に加えて同じフォルダーの `Roll-a-Ball_Data`、`UnityPlayer.dll`、`MonoBleedingEdge` などの実行用データも含めます。
Unity が出力する `*_BurstDebugInformation_DoNotShip` は配布対象に含める必要はありません。

## ビルド設定

- 対象: Windows 64bit / Player
- 初期解像度: 1920×1080。モニターの解像度を初期値として使う設定は無効にします
- 全画面モード・描画品質・スクリプト実行方式: 既存の Player / Quality Settings を使用します
- シーン: Build Profiles の **共通 Scene List** で有効なものを、登録順に含めます。先頭が起動シーンです
- Development Build: 必要なときだけ有効にします。「ビルドして起動」は成功後の自動起動を追加します

このツールは個別 Build Profile の Scene List 上書きではなく、ウィンドウに表示する共通 Scene List を使用します。
解像度設定の適用時に Player Settings と変更済みアセットを保存します。
未保存シーンがある場合は Unity の標準保存確認が開き、キャンセルするとビルドも中止します。

## 実行できない場合

- 再生・コンパイル・アセットインポート・他のビルド中は、完了後に操作します
- シーン未登録や削除済みシーンがある場合は、Build Profiles の共通 Scene List を修正します
- Windows Build Support がない場合は、Unity Hub で使用中の Editor に対応モジュールを追加します
- プロジェクト内の出力先は `Build/` または `Builds/` 配下にします。プロジェクト外の出力フォルダーも選べます
- ビルド失敗時はウィンドウの結果と Console を確認します

実装は `Assets/Editor/WindowsBuildWindow.cs` と `WindowsBuildUtility.cs`、表示スタイルは `WindowsBuildWindow.uss` です。
ツール自体は Editor 専用で、生成した Player には含まれません。
