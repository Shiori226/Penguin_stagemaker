# SlidingPenguin

[English version README](https://github.com/KouyamaCreate/SlidingPenguinMaker/blob/main/README.md)

SlidingPenguinは、海に落ちないように氷の上のペンギンを操作し、ゴールを目指すドライビングゲームです。

そのフォークプロジェクトである本リポジトリ（SlidingPenguinMaker）は、SlidingPenguinの本編に加えてゲーム内ですぐ組んで遊べるステージメーカー機能を搭載したものです。かなり乱雑に変更を加えているため、現時点では可読性の観点から本家リポジトリへのマージは考えておりませんが、本家側に変更があった場合は積極的にこちらにも取り込んでいこうと考えています。

ステージはツルツル滑り、自動で進んでいきます。左右上下の移動と加速を駆使して、ゲームオーバーにならないようにペンギンを制御しましょう。

実験でも利用しやすいよう、理解しやすく、少ない入力で体験可能なゲームとなっています。

![SlidingPenguin](https://user-images.githubusercontent.com/71160720/222035563-a278f647-33f1-4d93-ba49-c4aefe5f5602.jpg)


## ゲーム詳細


### ルール

クリア条件
- 操作するペンギンが、海に落ちたり、画面外にいくことなくゴールに辿り着く

ゲームオーバー条件
- ペンギンの体が氷の外に出て、海に落ちる
- 設定された制限時間をオーバーする

タスク
- 道中に落ちている魚を集める。ゴールまで辿り着ことが最優先。



### 操作方法

- キーボード操作
![figure_page-0003](https://github.com/open-video-game-library/SlidingPenguin/assets/63552585/e6ccdd2d-61e6-41f3-bcfc-f93f52822446)


- ゲームパッド操作
![figure_page-0001](https://github.com/open-video-game-library/SlidingPenguin/assets/63552585/09d4a778-8899-4336-9d85-5fad01d06a8f)


## Features

パラメータ調整機能
- 移動感度
- 制限時間

パラメータ出力機能
- 成否
- 入手したサカナの数
- クリア時間
- 移動距離
- 移動感度
- 制限時間


### 研究利用例

ゲーム体験の主観評価法の提案研究
- 感情の変化が現れやすいゲームであり、かつ学習しやすいため、実験利用に適している。

コントローラデバイスの提案研究
- 自動的に進み、かつツルツル滑る氷のステージの特性から、連続的な制御を必要とするゲームになっているため、コントローラデバイスの評価に適している。

## Requirement

Unity2022.3.24f1


## Licence

本コンテンツは、[MITライセンス](https://github.com/open-video-game-library/SlidingPenguin/blob/main/LICENSE.md)のもとで利用が許可されています。

## Use in Research

[本コンテンツを研究利用する場合の推奨事項](https://github.com/open-video-game-library/SlidingPenguin/blob/main/RESEARCH_USE.JP.md)

## Contact

意見や要望、質問などがありましたら、[こちら](https://openvideogame.cc/contact)からお問い合わせ下さい。

