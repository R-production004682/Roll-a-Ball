# WebGL 公開・Discord 通知の確認

確認日: 2026-10-10 / Unity 6000.3.20f1 / Windows

## 変更内容

- WebGL 専用ビルドウィンドウと、既存 Windows ウィンドウから開くボタンを追加
- 共通 Scene List / WebGL / 1920×1080 / Gzip フォールバックでビルド
- 専用 Web テンプレートで内部描画サイズを固定し、ブラウザーの表示領域に全体を収める
- GitHub CLI の認証を利用し、生成した Player だけを公開専用ブランチへアップロード
- GitHub Pages の公開・HTML・ビルド識別情報の到達後に Discord へ通知
- 指定ロールのメンションだけを許可し、Discord の送信確認と返された mention_roles を検証
- Webhook は Git 対象外の UserSettings に Windows ユーザーに紐付けて暗号化保存

## 実施した確認

- Web Build Support を使用中の Editor に追加
- 作業用チェックアウトの Git LFS アセットを取得後、Unity のコンパイルと WebGL ビルドに成功
- 初回ビルド約 287.5 秒、専用テンプレート反映後のビルド約 14.6 秒
- 新規 C# ファイルと Web テンプレート・フォルダーの .meta を Unity が生成
- Windows PowerShell 5.1 で公開スクリプトとオフラインテストを実行
- 公開対象のフォルダー境界、不正ロール、特定ロールの allowed_mentions、公開成功時のみの通知、過去版の保持をモックで確認
- GitHub API の未作成ブランチ HTTP 404 を実際の読み取り API で確認
- ローカルの暗号化 Webhook 読み込みと公開設定の検証に成功
- GitHub Pages の legacy 公開元を webgl-builds / に設定し、2 件のビルドを公開
- 各公開について HTML と build-info.json の到達を確認し、Discord への送信・指定ロールの mention_roles を確認
- 最終版のブラウザーでタイトル表示、クリックによるステージ選択・ステージ開始、W キーによるプレイヤー移動を確認
- 最終版の Canvas 内部サイズ 1920×1080、表示幅約 896 px、ページの横方向のはみ出しなし
- ブラウザーのエラーログなし
- git diff --check 成功。Webhook 設定が git check-ignore の対象であることを確認

初回ビルドでは LFS の DLL がポインタのままだったため DOTween のコンパイルが失敗した。git lfs pull で復元してから成功を確認した

公開先: https://r-production004682.github.io/Roll-a-Ball/

最終確認版: https://r-production004682.github.io/Roll-a-Ball/builds/20261010-092215-fab57a/

Discord ロール ID: 1538587867746668554

## 未実施の確認

- Unity の新規ウィンドウをマウスで操作するビルド・公開（今回は CLI と同じビルド・公開処理を実行）
- 別の PC・別の Discord 受信者の端末通知、スマートフォンのゲーム操作
- 全ステージのクリア、ブラウザー間の保存互換性

受信者の端末への通知方法は Discord 側の通知設定による

## PR 分割後の確認

- `feature/build-webgl-publishing` は master から独立して作成。Coin の HUD と Title の入力・演出変更は含めない。
- 分割後のブランチのソースを抽出し、Unity 6000.3.20f1 の C# コンパイラーと既存の Editor 参照を使用して runtime 120 ファイル・Editor 15 ファイルを個別にコンパイル。両方成功。
- 新規アセットの GUID 参照と `git diff --check` を確認。Coin 側との変更ファイルの重複なし。
- 実ビルド・公開・ブラウザー確認は分割前の作業環境で実施。上記の公開版には Coin と Title の変更も含まれる。
- 分割後ブランチ単独の Unity インポート・Player の実ビルドは未実施。
