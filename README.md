Penetrator

## 概要

Unityでゲームを制作する際に、GoogleSpreadSheet等で大量に制作するマスタデータを少ない労力でゲームに組み込む為のツールです

- 機能
  - スキーマ構造の一括管理
    - マスタテーブルの構造はC#コード(class/struct)で一元管理
      - class/structの1インスタンスが1レコード、そのインスタンスの配列1つが1テーブル
      - RDBのような行と列を持つテーブルが基本構造
  - GoogleSpreadSheet上で作成したマスタデータをUnityのEditor上で取得/保存が可能
    - 1Sheet1テーブルとして、テーブルごとにJsonファイルとして格納
    - データ構造は、Sheet名がテーブル名、列名が前述のclass/structのField/Propertyに対応
  - Attributeによる汎用データ整合性チェック
  - class/structのField/Propertyに専用のAttributeをセットすることで、入力データに制約を設けることができる

- 設計コンセプト
  - データ構造とデータ原本の管理をDRYにする
    - 各所で応急処置を行った結果、データ修正が職人芸になってしまう状況を避けたい
    - マスタデータの修正はスプレッドシートに、データ構造はコードに集約
  - データの整合性チェックを常に行えるようにしたい
    - マスターデータ不整合による不具合に悩まされたくない
    - Editor拡張、テスト、ゲーム中のどこでもチェック処理を実行可能

## 使い方

### 事前準備と初回同期まで

1. (初回のみ)Google Cloud PlatformのOAuth ClientID生成

Unity EditorからGoogle SpreadSheetにアクセスするために、OAuthのClientID設定をGoogle Cloud Platfrom(以下GCP)の管理コンソール上で作成する

※ 接続用のGoogleアカウントが必要になるため、用意しておくこと。この仕組みで管理するGoogle SpreadSheetにはこのアカウントでアクセスできる権限が必要になるため、開発プロジェクトの共有アカウント等を使用するのが望ましい
※ 2021/04/16現在。GCP管理コンソールの実装は度々変わるため、操作に違和感があればググることを推奨する
※ GCPのプロジェクトを作っていない場合、GCPの管理コンソールへログイン後にプロジェクトを作る必要あり

1.1. GCPの管理コンソールをブラウザで開く

GCP 管理コンソール(認証情報)
https://console.cloud.google.com/apis/credential

管理コンソールを開く際に、Googleアカウントでのログインを要求されるため、ログインする

1.2.  画面中央上の「認証情報を作成」、「OAuth クライアント ID」を選択
1.3. 「OAuthクライアントIDの作成」画面で、「アプリケーションの種類」から「デスクトップアプリ」を選択
1.4. 「作成」ボタンを押す
1.5. 「認証情報」画面に戻ると、画面中段の「OAuth 2.0 クライアントID」に先ほど作成した設定が追加されている。追加された設定行の末尾にダウンロードボタンがあるので押す。
1.6. 設定ファイル(client_secret_～.json)

2. 管理対象のSpreadSheetの準備

SpreadSheetをGoogle Drive上に用意する

用意したSpreadSheetからSheetIDをメモしておく
SheetIDは、Google SpreadSheetのURLから下記の(SheetID)部分を抜き取れば良い

https://docs.google.com/spreadsheets/d/(SheetID)/edit#gid=0

例:
https://docs.google.com/spreadsheets/d/xyz12345111111111111111111/edit#gid=0
なら
xyz12345111111111111111111

この仕組みではSpreadSheet側に特殊な仕掛けはないため、SpreadSheetの形式に制約はない
(アクセス権限のみ注意)

3. 開発用のUnityプロジェクトを用意し、Unityで開く
4. PenetratorのUnityPackageを開き、importする

Penetrator_(ver).unitypackage

5. OAuth Client設定ファイルをUnityProjectに取り込む

1.で取得した設定ファイル(client_secret_～.json)を MasterBuilderEditorWindowConfig.jsonにリネームし、 プロジェクトのAssets/Editor/Config/MasterBuilder/に格納する

6. プロジェクト用の設定クラスを作成

MasterBuilderConfigインタフェースを実装したクラスを、Unityプロジェクト1つにつき1クラス作成する
このクラスに管理対象のテーブル構造とテーブル名を記載する


設定例(テスト用):
Assets/Tests/EditMode/Penetrator/DataPool/Test/MasterBuilderConfigImplForTest.cs
上記設定に対応するSpreadSheet
https://docs.google.com/spreadsheets/d/1Go5YXCeBYCY7c2-8r7M5Hn3EF0tbWHYzVcSF1yd7onA/edit#gid=0

- 補足事項
  - ここで作成した設定クラスのFQDNはメモしておくこと。
    - 下記設定例(MasterBuilderConfigImplForTest)の場合、Penetrator.DataPool.Test.MasterBuilderConfigImplForTest
  - MasterBuilderConfig.MasterTables()が返す型が、この仕組みでSpreadSheetからのデータ同期対象となる型。記載がない場合取得対象外となるので注意
  - MasterBuilderConfig.MasterTables()が返却する型は、下記の規則に従うこと
    - Serializable Attributeを持つこと(JSONで保存するために必要)
    - 型名がGoogleSpreadSheet側のシート名と同一であること(例 TestData1 クラスはTestData1シートと対応する)
    - 同期対象にしたいフィールド/プロパティ名は、SpreadSheetの1行目に記載した列名と合致すること(例 TestData1シートのA1セルがId,B1セルがNameだった場合、TestData1クラスのId Field/PropertyにA列、Name Field/PropertyにB列の値が同期する)
    - 同期対象にしたいフィールド/プロパティ名はアクセス修飾子publicであること

7. Unity Editor上で、画面上部のメニューから「Window」「Penetrator」「MasterBuilder」を開く
8. 同期設定の作成

下記操作を行い、「設定保存」ボタンを押す
- 設定クラス欄に6.でメモしたクラスのFQDNを記入する
- SpreadSheetIDに2.でメモした管理対象のSheetIDを記載する

9. 「マスタ同期」ボタンを押すと、マスタの同期が行われ、SpreadSheetの内容がJsonファイルとして出力される

出力先はAssets/Config/DataPool/
失敗する場合はConsoleに出るエラーログを確認
(データの不整合やアクセス先のシート権限の不備等がある場合、ここで発覚する)

※ 初回実行時は、ブラウザが起動しGoogleアカウントの認証が要求される。ここでの認証は、作業者個人のアカウントで行う

10. 以後、「マスタ同期」ボタンを押すごとに、SpreadSheetの最新の内容がjsonとして出力されるようになる

### データ整合性チェック

- Field/Propertyに特定のAttributeを宣言することで、データの同期時に整合性のチェックを行ってくれる。データ内容が制約に違反する場合、同期処理時にエラーを吐く

- 使用できる制約の例
  - NotDefault
    - 宣言したFiled/Propertyがデフォルト値の場合エラー
      - 主にデータの記入漏れを検知する
      - 数値型の場合は0, 文字列型の場合はnullか空文字の場合にエラー
      - 参照型の場合はnullの場合にエラー
      - Enum型に設定する場合は、1番目の要素(intに変換されると0)をデフォルト値としてみなす。
    - Enum型の場合は、デフォルト値か値域外(セルの値が変換不能)の場合にエラー
  - Unique
    - Field/Propertyに設定した値がテーブル内(同一シート内)で重複した場合にエラー
    - Id等の識別子に使用する想定
  - ForeignKey
    - Field/Propertyに設定した値が指定したテーブルの指定列に存在しない場合にエラー
    - 参照先は、Attribute宣言で指定する(参照先のテーブル型(ReferenceTableType)、列名(ColumnName))
    - 他のマスタデータへの依存関係がある場合に使用
