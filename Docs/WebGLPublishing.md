# WebGL の公開・通知

## 用途と準備

Unity の **Tools → Roll-a-Ball → Build WebGL and Publish** から、1920 × 1080 の WebGL をビルドし、GitHub Pages へ公開できます。公開したページと build-info.json の到達を確認した後、設定した Discord ロールにプレイ URL を通知します。

- 対象 Editor に Unity Hub で Web Build Support を追加してください。追加後は Editor を再起動します。
- Git と GitHub CLI をインストールし、`gh auth login --hostname github.com` で認証してください。SourceTree などの Git 認証と gh の認証は別です。
- 公開先リポジトリへの書き込み権限と、初回の GitHub Pages 作成に必要な権限が必要です。既存の Pages が別の公開元を使用していれば停止します。
- Discord の通知先で Incoming Webhook を作成し、通知したいロールの ID とメンション許可を確認します。Webhook は次のコマンドで入力します。URL をコマンドの引数や Git 管理ファイルへ書かないでください。

リポジトリ直下で実行します。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\PublishWebGL.ps1 -ConfigureWebhook
```

Webhook は Windows のユーザーに紐付けて暗号化し、`Roll-a-Ball/UserSettings/DiscordWebhook.xml` に保存します。UserSettings は Git の追跡対象外です。

## 操作と公開対象

1. Unity の公開ウィンドウで GitHub の `owner/repository` と Discord Role ID を確認します。
2. 「WebGL ビルドのみ」で生成できます。「WebGL ビルド・公開・通知」はビルド成功後に転送、Pages の到達確認、Discord 通知まで実行します。
3. Console とウィンドウに公開ファイル数、合計サイズ、各ファイルのサイズ、進捗を表示します。

公開できるフォルダーは、このプロジェクトの `Roll-a-Ball/Builds/WebGL` だけです。リンクを含む出力は拒否します。転送対象は次に限定します。

- `index.html` と、その HTML が参照する `Build/` の現行ファイル（loader、data、framework、wasm など）
- `TemplateData/` と `StreamingAssets/` の実行用ファイル
- ツールが生成する `.nojekyll`、公開元マーカー、リダイレクト、版ごとの `build-info.json`

HTML が参照しない旧ハッシュ名のビルド、参照されていないシンボル、バックアップ、隠しファイル、meta、ソース、ログ、UserSettings、Library などは転送しません。StreamingAssets の実行データは自動で全て必要とみなすため、その内容を整理するときはゲーム側の参照を確認してください。

転送前に 1 ファイル 95 MiB 以下、現行版の合計 850 MiB 以下を検証します。配信する版は現行版と過去二版で、合計 900 MiB 未満に収まるよう過去版を減らします。過去版を配信対象から外しても Git のコミット履歴内のデータは残ります。長期運用では公開ブランチの履歴も容量確認が必要です。

## 転送の仕組みとエラー

大きなビルドは GitHub の blob 作成 REST API へ Base64 で送らず、Git で転送します。Base64 の約 33% のサイズ増加と、大きな JSON リクエストの生成を避けます。ゲーム自体のビルドサイズや画質を変える処理ではありません。

`Roll-a-Ball/Builds/Publishing/git-<ランダム値>/` に新規 Git 領域を作成し、公開ブランチ `webgl-builds` だけを取得します。開発リポジトリの index、作業ファイル、ブランチ、コミット履歴を公開に使いません。Git の HTTPS 認証もコマンド単位で `gh auth git-credential` を使うため、グローバルの認証設定を書き換えません。更新は通常の push で行い、同時公開による競合を強制上書きしません。

成功した Git 作業領域は削除します。失敗時は調査用に残ることがあります。対象は Builds 配下のツール生成物です。Git と API の直近のエラーは同じ Publishing 配下の `git-error.txt` と `github-error.txt` に記録します。

- HTTP 401 / 403: gh の認証、権限、API 制限を確認します。
- HTTP 404: リポジトリ・公開設定を確認します。初回の公開ブランチと Pages が存在しないことは正常です。
- HTTP 413 / 422: 送信サイズ、入力内容、更新競合を確認します。以前の実装は全 API エラーを認証・権限の問題として表示していました。
- Git の転送失敗: エラーファイル、`gh auth status`、通信状態、同時公開を確認します。
- Pages の失敗・タイムアウト: エラーに表示される Actions の実行 URL を確認します。公開対象コミットの最新の Pages Actions が成功し、ページと build-info.json の到達確認が完了しなければ Discord 通知は送りません。
- Discord 通知失敗: メッセージ送信の失敗とロールメンションが受理されない場合を区別します。受け付けたメッセージ ID・チャンネル ID・ロール確認結果は `Roll-a-Ball/Builds/Publishing/notification-result.json` に記録します。Webhook の URL やトークンは記録しません。

Pages のビルドは、公開ブランチへの push または初回の Pages 作成によって自動起動します。追加のビルド要求は送りません。二重起動時にキャンセルされた過去の実行や、旧 Pages ビルド API の `errored` 表示で、進行中のデプロイを失敗扱いしないようにしています。Actions の起動待ち・進行中・キャンセル後の置換実行待ちは最大 10 分待機し、最新の実行が実際に失敗した場合は実行 URL と結果を表示します。

## 公開済みの最新版に通知だけを送る

通知処理は Unity から起動するローカル PowerShell 内で実行します。処理がエラーで終了した後に GitHub 側でデプロイが完了しても、自動では再開されません。次のコマンドで最新版のデプロイとページ到達を検証し、再ビルド・push せずに通知できます。この操作は Discord に実際のメッセージとロールメンションを送るため、送信済みの場合は重複通知になります。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\PublishWebGL.ps1 -NotifyLatest
```

公開先やロールを変更して使用していた場合は、ウィンドウと同じ `-Repository owner/repository -RoleId ロールID` を指定します。

メッセージが届いてもメンションが効かない場合は、対象ロールの ID・サーバー・メンション許可を確認します。ロールメンションが Discord に受理されたことと、各メンバーにデスクトップ・プッシュ通知が届くことは別で、後者はユーザー側の通知設定にも影響されます。

## 外部公開を行わない確認

次の検証は Webhook の読み取り・アップロード・Discord 通知を行いません。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\PublishWebGL.ps1 -OutputDirectory "$PWD\Roll-a-Ball\Builds\WebGL" -ValidateOnly
```

次の回帰テストはローカルの bare リポジトリへ実際に 55 MiB のバイナリを転送し、初回・再公開、過去版保持、不要ファイル除外、失敗時の通知抑止を確認します。GitHub API と Discord はモックで、外部に公開・通知しません。Windows PowerShell 5.1 で実行できます。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Tests\PublishWebGL.Tests.ps1
```
