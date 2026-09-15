# 共通ゲームデータ

`GameDataManager` はシーンをまたいで共有するゲーム進行データを管理します。設定音量は既存の `GameSettings` / PlayerPrefs に残し、この JSON セーブには含めません。

## 保存内容

| データ | 初期値・ルール |
|---|---|
| 所持金 | 0。加算・減算後も負数にならず、処理結果を `out` 残高で返す |
| ステージ | 安定 ID で解放・クリア状態を保持。初期 ID は `stage-1` |
| クリアタイム | 秒単位の昇順上位5件。同タイムでは先に保存された記録を上位にする |
| 購入済み商品 | 安定した商品 ID の一覧。購入時に残高減算と所有記録を同じ保存として処理 |

セーブは `Application.persistentDataPath/roll-a-ball-save.json` に保存し、版番号は `GameSaveData.CurrentVersion` が管理します。置換前のファイルは `.backup` に残します。正常な変更はその都度保存します。

ステージの順序やクリア報酬額は定義していません。ゲーム進行側が `UnlockStage` で解放し、報酬がある場合は `TryAddCurrency` を呼び出してください。`OutGameClearController` は Inspector の `stageId`（既定 `stage-1`）でクリアとタイムを記録します。

保存が破損している場合や未対応の版の場合は初期値で動作し、既存ファイルへの書き込みを止めます。`HasLoadFailure` で状態を確認できます。`ResetToDefaults` はゲーム進行データを明示的に初期化し、既存ファイルを `.backup` に退避します。

## 呼び出し例

```csharp
if (GameDataManager.TryAddCurrency(100, out var balance))
{
    Debug.Log($"所持金: {balance}");
}

if (GameDataManager.TryPurchaseItem("skin.red", 50, out balance))
{
    Debug.Log($"購入後の所持金: {balance}");
}

GameDataManager.UnlockStage("stage-2");
if (GameDataManager.RecordStageClear("stage-2", 42.5f, out var rank))
{
    Debug.Log($"クリア記録の順位: {rank}");
}
```

`RecordStageClear` の `rank` は上位5件に入ったとき1～5、入らなかったとき0です。戻り値はクリア状態を保存できたかを示します。

## 確認手順

実セーブを保護するため、保存・破損テストは専用のユーザープロファイルまたはバックアップしたセーブで行います。

1. `Assets/Scenes/TestScene/OutGameTestScene.unity` を開いて Play し、初回状態が所持金0・`stage-1` 解放済みであることを確認します。
2. 一時的なテスト呼び出しから `TryAddCurrency`、`TrySpendCurrency`、`TryPurchaseItem`、`UnlockStage` を実行し、戻り値・残高・所持状態を確認します。特に残高不足、重複購入、残高の負数化が起きないことを確認します。
3. `stage-2` を解放し、異なるタイムと同タイムを含めて6回以上 `RecordStageClear` を呼びます。`GetTopClearTimes` が昇順5件になり、同タイムでは先の記録が残ることを確認します。
4. Play を停止して再起動し、所持金・ステージ・タイム・商品状態が復元することを確認します。
5. 専用プロファイルの JSON を破損させて起動し、`HasLoadFailure` が true になり元ファイルが変わらないことを確認します。続けて `ResetToDefaults` を呼び、初期値と `.backup` が作成されることを確認します。
6. Settings Dialog の音量を変更して閉じ、再起動後にも復元することを確認します。音量は従来どおり PlayerPrefs に保存されます。
