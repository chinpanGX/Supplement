<h1 align="center">Supplement</h1>

[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE.md)

## 概要
Unityにおける補助的なスクリプト群を提供するパッケージです。
> 本パッケージは [UniTask](https://github.com/Cysharp/UniTask), [VContainer](https://github.com/hadashiA/VContainer), [Addressables](https://docs.unity3d.com/Packages/com.unity.addressables@latest) に依存しています。

### 主な機能 
#### 【Core】 
- Unity に依存しない「暗号化・永続化・メッセージング・リスト等の共通ユーティリティ」と、抽象インターフェース

#### 【Unity】
- Unityに依存をする階層メッセージング、コンポーネント拡張の実装
- Core の抽象を実装した「ファイル IO、暗号化付きストレージ」

#### 【Loader】
- アセットハンドルとローダーの抽象インターフェース（Abstractions）と Addressables 実装

#### 【VContainer】
- Core, Unity, Loader の各機能を VContainer の DI コンテナに登録するための拡張メソッドの定義
- IObjectResolverにstaticメソッドでアクセスするためのGatewayクラス

### インストール方法
Package Managerの「Add package from git URL...」から以下のURLを入力してインストールしてください。
```
https://github.com/chinpanGX/Supplement.git?path=Assets/Supplement
```

## 他のパッケージとの連携
### 【Supplement ZeroMessanger】
Supplement のメッセージング機能を ZeroMessanger を使って実装したパッケージです。
#### インストール方法
Package Managerの「Add package from git URL...」から以下のURLを入力してインストールしてください。
```
https://github.com/chinpanGX/Supplement.git?path=Assets/Supplement.ZeroMessenger
```

> [!TIP]
> Assets/Supplement.Test フォルダにテストコードを配置していますので、参考にしてください。

## ライセンス
本ソフトウェアはMITライセンスで公開しています。

https://github.com/chinpanGX/Supplement/blob/main/LICENSE