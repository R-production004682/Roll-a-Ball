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

## Git の stash・ブランチ切り替えで止まる場合

### テストファイルの Permission denied

`Tools/Tests/PublishWebGL.Tests.ps1: Permission denied` は、Git がその端末のファイルへアクセスできなかったことを示します。画像や改行警告だけでは、ファイルロック・NTFS 権限・セキュリティソフトのどれかは断定できません。テストは自動実行されず、このファイルは公開する WebGL にも含まれません。

まず読み取り専用の診断を実行してください。ファイル内容、Webhook、認証トークンは出力しません。ファイル・ACL・Git 設定・index を変更せず、現在の stash にも触れません。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Check-WebGLTestFile.ps1
```

- `CanRead: false` の場合は `ReadError` の例外型・HResult・メッセージを確認します。まずテストファイルを開いているアプリを閉じ、同じ診断を再実行します。Windows セキュリティの「保護の履歴」や使用中のセキュリティソフトの記録も、対象パスと発生時刻で確認してください。検出を確認できていない状態で、誤検知とは判断しません。
- ACL の拒否や所有者の問題が確認できた場合は、その端末の管理者に対象ファイルの権限を確認してもらいます。リポジトリ全体への権限リセット、全員への書き込み許可、保護機能の無効化は行いません。検出が確認できた場合は、検出名と該当ファイルを確認し、必要に応じて製品提供元へ誤検知の調査を依頼します。
- `CanRead: true` で `ContentMatchesIndex: true` なら、Git が正規化した内容は index と同じです。`false` なら実際の変更があるので、内容を確認しバックアップまたは通常の stash で保管してください。ステージ済みの変更はこの判定とは別に確認します。
- SourceTree の組み込み Git とターミナルの Git が異なる場合は、診断の `-GitExecutable 'Git の exe パス'` で同じ実行ファイルでも確認できます。

この修正では Tools 配下の PowerShell の改行を LF に固定します。個人の `core.autocrlf` や他のアセットの属性は変更しません。これは改行だけの差分の予防であり、OS が拒否するアクセス権を変更する処理ではありません。

修正ブランチを取得できず診断スクリプトがまだ手元にない場合は、次の読み取りだけでもエラーを確認できます。

```powershell
Get-Item -LiteralPath .\Tools\Tests\PublishWebGL.Tests.ps1 | Format-List FullName,Attributes,Length
[IO.File]::ReadAllBytes((Join-Path $PWD 'Tools/Tests/PublishWebGL.Tests.ps1')).Length
git status --short -- Tools/Tests/PublishWebGL.Tests.ps1
git ls-files --eol -- Tools/Tests/PublishWebGL.Tests.ps1
```

読み取りが回復し、テストファイルを HEAD の状態へ戻したい場合に限り、まずリポジトリ外にコピーして内容を確認します。次の処理はファイルを読めなければ停止します。既存のコピーを上書きせず、他のソースや index は戻しません。

```powershell
$ErrorActionPreference = 'Stop'
$testFile = '.\Tools\Tests\PublishWebGL.Tests.ps1'
$backupFolder = Join-Path (Split-Path $PWD -Parent) ('RollABall-GitRecovery-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $backupFolder | Out-Null
$backupFile = Join-Path $backupFolder 'PublishWebGL.Tests.ps1'
Copy-Item -LiteralPath $testFile -Destination $backupFile -ErrorAction Stop
if ((Get-FileHash -LiteralPath $testFile).Hash -ne (Get-FileHash -LiteralPath $backupFile).Hash) { throw 'Backup verification failed' }
git restore --source=HEAD --worktree -- Tools/Tests/PublishWebGL.Tests.ps1
```

バックアップには実際の変更が残ります。復旧できない場合はそのまま保管し、強制 checkout、`reset --hard`、`clean -fdx` などで作業を消さないでください。

### SourceTree の stash が Library まで処理している

`git stash push --all` は `.gitignore` の対象も退避・削除します。Unity が使用中の Library、Temp、ビルド出力やローカル設定まで巻き込むため、普段の作業退避には使いません。Library の CRLF 警告は、これらを取り込もうとしているサインで、上記の Permission denied の原因とは別です。

SourceTree で無視ファイルを含める設定を外すか、ターミナルで次を使います。未追跡のソースも含め、無視ファイルは残します。アクセス拒否が継続しているファイルを読めるようにするコマンドではないため、先に上記の診断を済ませてください。

```powershell
git stash push --include-untracked -m "作業の一時退避"
git stash list
```

復元はまず `git stash apply 'stash@{0}'` で内容を確認します。成功するまでは stash を削除しません。既に失敗した stash が一部作成されている可能性もあるため、最初に `git stash list` を確認してください。

### ブランチが別の worktree で使用中

同じ端末の `git worktree list` に表示されるチェックアウトが、そのブランチを占有しています。リモートや共同開発者の端末の設定が原因ではありません。

```powershell
git worktree list
git -C '表示された worktree のパス' status --short --branch
git -C '表示された worktree のパス' switch --detach
```

`switch --detach` は同じコミットでブランチを解放し、そこにある未コミット変更とブランチ自体を残します。作業中の別ウィンドウがある場合はそこでの編集を止めてから操作します。worktree のフォルダーを直接消したり、強制削除する必要はありません。ブランチ削除が未マージで拒否された場合も、コミットを確認せず `-D` に置き換えないでください。

仕様の参照先: [git stash](https://git-scm.com/docs/git-stash)、[git worktree](https://git-scm.com/docs/git-worktree)、[改行属性](https://git-scm.com/docs/gitattributes)、[Windows の保護の履歴](https://support.microsoft.com/en-us/windows/security/windows-security/protection-history-in-the-windows-security-app)
