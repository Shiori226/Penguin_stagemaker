# プレイログの見方

プレイログは、ゲームを1試行プレイしてクリアまたはゲームオーバーになったあと、結果画面のデータ出力ボタンから出力します。

## 保存場所

Windowsでは、通常次のフォルダに保存されます。

```text
%USERPROFILE%\AppData\LocalLow\OVGL\SlidingPenguin\
```

エクスプローラーのアドレスバーに上記を貼り付けてください。`log_YYYYMMDD_HHmmss`というフォルダが出力1回ごとに作成されます。

例：

```text
C:\Users\keita\AppData\LocalLow\OVGL\SlidingPenguin\log_20260803_144045\
```

## 出力ファイル

| ファイル | 内容 |
| --- | --- |
| `events_trial1.csv` | 入力、衝突、魚取得、落下など、発生したイベントの履歴 |
| `stream_trial1.csv` | 毎フレームのペンギン位置・速度・入力方向など |
| `snapshot.csv` | 試行終了時のスコア・取得数・衝突数などの集計値 |
| `stage.json` | プレイしたステージの情報 |

複数試行を続けて出力した場合、`trial2`、`trial3`のファイルも作られます。

## まず見るファイル

最初は`events_trial1.csv`を見てください。

Excelで開き、1行目のフィルターを有効にして、`event_type`を選択すると確認しやすくなります。

### 入力を見る

`event_type`を`input`に絞ります。

| 列 | 内容 |
| --- | --- |
| `input_name` | 操作の種類 |
| `input_phase` | 押した・離した・開始時に押されていた状態 |
| `time` | 試行開始からの経過秒数 |
| `frame` | 記録フレーム |

`input_name`には次の値が入ります。

| 値 | 内容 |
| --- | --- |
| `move_left` | 左方向 |
| `move_right` | 右方向 |
| `move_up` | 上方向 |
| `move_down` | 下方向 |
| `accelerate` | 加速 |
| `pause` | ポーズ |

`input_phase`の意味は次のとおりです。

- `down`：押した
- `up`：離した
- `held`：試行開始時点ですでに押されていた

例えば、次の2行があった場合、加速を約0.217秒押しています。

```text
accelerate,down,0.492
accelerate,up,0.709
```

現在の`accelerate`はUnityの`Submit`入力を記録しています。`Submit`にはEnter、Return、Spaceが割り当てられているため、現状のログではこの3つを区別できません。

### 魚取得を見る

`event_type`を`fish_collected`に絞ります。

- `time`：魚を取得した時刻
- `item_name`：魚の種類
- `position_x/y/z`：取得時のペンギン位置

### 衝突を見る

`event_type`には次の種類があります。

| 値 | 内容 |
| --- | --- |
| `seal_collision` | アザラシとの衝突 |
| `wall_collision` | 壁との衝突 |
| `wall_reflect` | 壁による反射 |
| `obstacle_collision` | その他の障害物との衝突 |
| `ice_reached` | 氷・足場への初回到達 |
| `checkpoint_reached` | チェックポイントへの到達 |

衝突イベントでは、必要に応じて次の列も確認できます。

- `position_x/y/z`：衝突位置
- `velocity_before_x/y/z`：衝突前の速度
- `velocity_after_x/y/z`：衝突後の速度
- `normal_x/y/z`：衝突面の法線方向
- `target_id`：対象オブジェクトの階層上のID

### 落下・ゴールを見る

| `event_type` | 内容 |
| --- | --- |
| `fall_detected` | 氷から離れて落下した |
| `respawn` | リスポーンした |
| `goal_enter` | ゴールに入った |
| `stage_clear` | ステージクリアが確定した |
| `time_up` | 制限時間を使い切った |
| `game_over` | ゲームオーバーになった |

## 集計値を見る

試行全体の結果は`snapshot.csv`を見ます。1試行につき1行が追加されます。

主な列は次のとおりです。

| 列 | 内容 |
| --- | --- |
| `stats_fish_normal` | 通常魚の取得数 |
| `stats_fish_gold` | 金魚の取得数 |
| `stats_fish_total` | 魚の合計取得数 |
| `stats_seal_collision` | アザラシ衝突回数 |
| `stats_wall_collision` | 壁衝突回数 |
| `stats_wall_reflect` | 壁反射回数 |
| `stats_obstacle_collision` | 障害物衝突回数 |
| `stats_fall_count` | 落下回数 |
| `stats_respawn_count` | リスポーン回数 |
| `stats_ice_reached` | 到達した氷の数 |
| `stats_checkpoint_reached` | 到達したチェックポイント数 |
| `stats_goal_reached` | ゴール到達の有無 |
| `stats_stage_cleared` | クリアの有無 |
| `stats_time_up` | タイムアップの有無 |
| `stats_first_fish_time` | 最初に魚を取得した時刻 |
| `stats_last_fish_time` | 最後に魚を取得した時刻 |
| `stats_first_ice_time` | 最初に氷へ到達した時刻 |
| `stats_goal_time` | ゴール到達時刻 |
| `stats_play_time` | 試行時間 |

個々の取得時刻や衝突時刻は`events_trial1.csv`、合計値は`snapshot.csv`という使い分けです。

## Streamの見方

`stream_trial1.csv`は、プレイ中の状態をフレームごとに記録したファイルです。

- `player_pos`：ペンギン位置
- `player_velocity`：ペンギン速度
- `input_direction`：そのフレームの移動方向
- `player_grounded`：氷・足場に接地しているか
- `time`：経過秒数
- `frame`：フレーム番号

ベクトル値は、1つのセルに`x;y;z`の形式で入っています。

## PowerShellで簡単に確認する方法

イベントの種類ごとの件数を確認する例です。

```powershell
$log = Import-Csv "C:\Users\keita\AppData\LocalLow\OVGL\SlidingPenguin\log_20260803_144045\events_trial1.csv"
$log | ForEach-Object { $_.event_type } | Group-Object | Sort-Object Count -Descending
```

入力イベントだけを確認する例です。

```powershell
$log | Where-Object { $_.event_type -eq "input" } |
    Select-Object time,frame,input_name,input_phase,input_value
```

## 注意点

- `events_trial1.csv`は発生したイベントだけを記録します。
- ペンギン位置を毎フレーム追いたい場合は`stream_trial1.csv`を使います。
- 集計値だけを使いたい場合は`snapshot.csv`を使います。
- 試行を途中でやめた場合、その試行のログは破棄され、出力対象になりません。
