---
name: release
description: "Supplementのリリースの準備をする。changelog・package.json・ドキュメント・コンパイルとテストを点検し、PRとタグの手前まで進める。引数にバージョン(1.5.0など)を渡す。省略すると、changelog.mdの未リリースのバージョンを使う。"
---

# release

リリースの準備を、漏れが無いように順に進める。**コミット・プッシュ・PRの作成・タグ付けは、ユーザーに確かめてから行う。**

## 対象のパッケージ

| フォルダ | パッケージ名 |
|---|---|
| `Assets/Supplement` | `com.chinpangx.supplement`(本体) |
| `Assets/Supplement.ZeroMessenger` | `com.chinpangx.supplement.zeromessenger` |
| `Assets/Supplement.MessagePack` | `com.chinpangx.supplement.messagepack` |

## 手順

### 1. 状態を確かめる

- `git status`と`git diff main...HEAD --stat`で、リリースに入る変更を把握する。未コミットの変更があれば、入れるかをユーザーに聞く
- `docs/maintainers/roadmap.md`の、そのバージョンの残りの項目を読む。終わっていない項目があれば、先に進めてよいかを聞く

### 2. バージョンをそろえる

- `docs/changelog.md`の「(未リリース)」の節を、そのバージョンにする。互換性が壊れる変更が「互換性が壊れる変更」に分かれ、理由が書いてあるかを確かめる
- 変更があったパッケージの`package.json`の`version`が、changelogと合っているかを確かめる。本体が互換性の壊れる変更を含むなら、別パッケージの`dependencies`の本体のバージョンも見直す
- 互換性が壊れる変更があるのにパッチのバージョンしか上げていない場合は、ユーザーに伝えて判断を仰ぐ

### 3. ドキュメントを点検する

- `docs-lint`スキルを、引数なしで(すべてのページを)実行する
- `docs/getting-started.md`のタグの書き方(`#v1.5.0`など)が、実際に付けるタグの形と合っているかを確かめる

### 4. コンパイルとテスト

- `docs/maintainers/testing.md`の手順で、Unity CLIを使ってコンパイルし、EditModeとPlayModeのテストを回す
- XMLドキュメントコメントの付け漏れを、`CLAUDE.md`の手順(`dotnet build ... -p:DocumentationFile=...`)で確かめる
- 失敗したら、ここで止めてユーザーに報告する

### 5. ベンチマーク

- `Benchmarks/`や、ベンチマークの対象(`Messaging/`など)が変わっていれば、`docs/maintainers/benchmark.md`の数値が古くないかを確かめる
- 計測は自分で実行しない。コマンドを示して、ユーザーに頼む

### 6. 仕上げ

- `docs/maintainers/roadmap.md`から、終わったバージョンの節を消す。持ち越す項目は次のバージョンの節に移す
- 変更をまとめて報告し、次をユーザーに確かめてから行う
  1. コミット(メッセージはこれまでの`vX.Y.Z: 概要`の形。本文にchangelogの要点)
  2. `main`へのPRの作成(`gh pr create`)
  3. マージの後のタグ付け(`git tag v<バージョン>`と`git push origin v<バージョン>`。タグには`v`を付ける)
