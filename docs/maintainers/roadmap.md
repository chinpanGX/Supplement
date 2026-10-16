# 作業の状態(メンテナー向け)

いま進めているバージョンと、残っている作業・決めていないことをまとめます。作業の区切りごとに更新し、終わった項目は消します(何をしたかは[変更履歴](../changelog.md)とgitの履歴に残ります)。

## 1.4.1(作業中)

ブランチは`feature/v1.4.1`です。中身は[変更履歴](../changelog.md)の未リリースの節を見てください。

- [ ] ベンチマークを回し直し、[ベンチマーク](benchmark.md)の数値・日付・環境を今のコードに合わせる。計測はメンテナーが行う
- [ ] リリースの手順(`/release`)で`main`に入れ、タグを付ける

## 決めていないこと

- 非推奨にした`GlobalMessageBroker`(別パッケージ`com.chinpangx.supplement.zeromessenger`)を、どのバージョンで削除するか。パッケージごと終わらせるか
- Unity上で`MessageBroker`を測るか(Performance TestingとIL2CPPのプレイヤーで)。測るなら`Packages/manifest.json`に依存が増えます

## やらないこと

[設計の方針](design-principles.md)を見てください。
