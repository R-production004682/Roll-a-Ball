# WebGL のビルド・公開・Discord 通知

Unity の **Tools → Roll-a-Ball → Build WebGL and Publish** を開きます。
Windows ビルドウィンドウの「WebGL のビルド・公開・Discord 通知を開く」からも開けます。

**WebGL ビルド・公開・通知** を押すと、次の順序で実行します。

1. 共通 Scene List の有効なシーンを WebGL / 1920×1080 でビルド
2. 完成したファイルだけを GitHub の `webgl-builds` ブランチへアップロード
3. GitHub Pages の公開と、HTML・ビルド識別情報への到達を確認
4. `@roll-a-ball` ロールへのメンションを付けて、プレイ URL を Discord に通知

ビルド失敗・キャンセル時は公開しません。公開の到達確認が失敗した場合は通知しません。
公開後に通知だけ失敗した場合は、結果表示に公開済み URL が残ります。
ブラウザーでの実際のプレイ確認は別途行ってください。

## 初期設定

- Unity 6000.3.20f1 に **Web Build Support** を追加します
- **GitHub CLI (`gh`)** をインストールし、`gh auth login` でログインします
- 初回に Pages を有効化するため、リポジトリの管理権限が必要です
- Windows PowerShell 5.1 を使用します
- Discord の `roll-a-ball` ロールで、メンションを許可します

Webhook はチャット・コード・GitHub の公開ブランチへ載せません。
次のコマンドで入力すると、入力を非表示にして Windows ユーザーに紐付けて暗号化保存します。

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./Tools/PublishWebGL.ps1 -ConfigureWebhook
```

保存先は Git 対象外の `Roll-a-Ball/UserSettings/DiscordWebhook.xml` です。
別の PC・Windows ユーザー・チェックアウトでは再設定が必要です。Webhook の変更時もこのコマンドを使います。

ウィンドウの公開リポジトリとロール ID はプロジェクトごとに記憶します。
初期値は `R-production004682/Roll-a-Ball` と `1538587867746668554` です。
通知では指定ロールだけを許可し、`@everyone`・`@here`・その他のロールには通知しません。
受信者の Discord 通知設定によって、端末への通知方法は異なります。

## 公開の扱い

- 出力先は `Roll-a-Ball/Builds/WebGL/` 固定です。ソース・UserSettings・秘密情報をアップロードしません
- Gzip 圧縮と Decompression Fallback を使用し、GitHub Pages の固定ヘッダーでも読み込める形式にします
- WebGL の出力サイズは 850 MiB 以下、単一ファイルは 95 MiB 以下に制限します
- WebGL 専用の描画サイズ・圧縮設定を Player Settings に保存します
- 専用 Web テンプレートで、内部描画を 1920×1080 に保ちながらブラウザーの表示領域に画面全体を収めます。全画面ボタンも利用できます
- 未保存シーンには Unity の標準保存確認を使用します
- 通知 URL はビルドごとの URL です。最新と過去 2 件を公開状態で残します。容量によって過去分を減らします
- 公開先のルート URL は最新ビルドへ転送します
- 既存 Pages が別の公開元を使用している場合や、公開専用ブランチが別用途だった場合は上書きせずに停止します
- GitHub 上には公開ブランチのコミット履歴が残ります。長期運用では容量の監視・整理が必要です
- ウィンドウを閉じても公開処理は継続します。Editor 終了やスクリプト再読み込みで処理が中断されるため、完了までは Editor を開いて待ちます

「WebGL ビルドのみ」はローカル出力まで実行し、公開・通知を行いません。
Windows のビルド操作は従来どおり使えます。

## CLI と確認

Unity CLI からのビルド例:

```powershell
unity run ./Roll-a-Ball --editor-version 6000.3.20f1 -- -buildTarget WebGL -executeMethod Roll_a_Ball.EditorTools.WebGLBuildUtility.BuildFromCommandLine
```

成功済みビルドの公開のみをやり直す場合:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File ./Tools/PublishWebGL.ps1 -OutputDirectory "$PWD/Roll-a-Ball/Builds/WebGL"
```

`-ValidateOnly` を追加すると、ローカルの出力・通知先の設定だけを検証します。公開・通知は行いません。

実装は `Assets/Editor/WebGLBuildWindow.cs`、`WebGLBuildUtility.cs`、`WebGLPublisher.cs` と `Tools/PublishWebGL.ps1` です。
Editor 拡張と秘密情報は生成した Player に含まれません。

参考: [Unity Web の公開](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-deploying.html)、[GitHub Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages)、[Discord Webhook](https://docs.discord.com/developers/resources/webhook)
