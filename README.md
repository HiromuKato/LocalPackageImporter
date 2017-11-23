# LocalPackageImporter
ロカールフォルダ(※)に保持するunitypackageを一覧表示しインポートを可能とするエディタ拡張  

（※)各環境のデフォルトのローカルフォルダは以下
- Window : C:/Users/(ユーザ名)/AppData/Roaming/Unity\Asset Store-5.x  
- Mac : /Users/(ユーザー名)/Library/Unity/Asset Store-5.x 

***DEMO***  
![demo](https://raw.githubusercontent.com/HiromuKato/LocalPackageImporter/media/media/localpackageimporter.gif)

## Requirement
- Unity 5.5以上

## Usage 
メニューの[Window] - [Local Package Importer]を選択すると起動します  

(画面上部の)[♡]ボタン：お気に入りunitypackageのみを表示します  
検索：インクリメンタルサーチします  
[Update metadata]ボタン：unitypackageのメタデータを取得・アップデートします 

各unitypackageごとの操作
- [Import]ボタン：unitypackageをインポートします  
- [Asset Store]ボタン：選択したunitypackageをAsset Storeで表示します  
- [♡]ボタン：お気に入りのON/OFFを設定します 

## Caution
初回起動時はunitypackageのメタデータを保持していないためアイコン等が表示されません。「Update metadata」ボタンを押すことで必要な情報を取得し以下記載のフォルダ配下に保存します。  
  
メタデータは全Unityプロジェクト共通で利用可能とするため以下に保持しています。本エディタ拡張が必要なくなった場合は以下のフォルダを削除してください。
- Windows : C:/Users/(ユーザ名)/Documents/LocalPackageImporter
- Mac : /Users/(ユーザー名)/Library/LocalPackageImporter

## Install
本リポジトリのAssets配下をプロジェクトに取り込むか、[LocalPackageImporter_v1.0.0.unitypackage](https://github.com/HiromuKato/LocalPackageImporter/blob/master/External/LocalPackageImporter_v1.0.0.unitypackage?raw=true)を利用して下さい。

## Auther
[Hiromu Kato](https://github.com/HiromuKato)

## Lisence
[MIT](https://github.com/HiromuKato/LocalPackageImporter/blob/master/LICENSE)