<h1 align="center">
  <img src=".github/images/icon.avif" width="64" aria-hidden="true" />
  <br />
  SQLiteアーカイブサーバー
</h1>

<p align="center">
  <a href="#クイックデモ">ク&#8288;イ&#8288;ッ&#8288;ク&#8288;デ&#8288;モ</a> ・
  <a href="#オーバービュー動機">オ&#8288;ー&#8288;バ&#8288;ー&#8288;ビ&#8288;ュ&#8288;ー&#8288;・&#8288;動&#8288;機</a> ・
  <a href="#オプション">オ&#8288;プ&#8288;シ&#8288;ョ&#8288;ン</a> ・
  <a href="#ftpサーバーの実行">FTP&#8288;サ&#8288;ー&#8288;バ&#8288;ー&#8288;の&#8288;実&#8288;行</a> ・
  <a href="#静的サイトの提供試験的">静&#8288;的&#8288;サ&#8288;イ&#8288;ト&#8288;の&#8288;提&#8288;供</a> ・
  <a href="README.md"><b>English</b></a>
</p>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset=".github/images/screenshot-dark.avif" />
  <img src=".github/images/screenshot-light.avif" alt="サーバーをブラウザで開くとアーカイブの内容をNginxやApacheのようなディレクトリリストとして見える" />
</picture>

## クイックデモ

SQLiteソースコードのターボール、じゃなくてスクラーボールをダウンしてブラウザで開いてみよう：

```
$ wget https://www.sqlite.org/src/sqlar/sqlite.sqlar
$ docker run -it --rm -v .:/srv -p 3939:80 ghcr.io/maxkagamine/sqlarserver sqlite.sqlar
$ open http://localhost:3939
```

## オーバービュー・動機

[SQLiteアーカイブ](https://sqlite.org/sqlar.html)（通称：sqlar）とはファイルをBLOBとして保存するための標準テーブルスキーマ<sup>１</sup>のあるただのSQLiteデータベースである。そうする理由はメインページがいくつか説明する（私見では、主のはリレーショナルかインデックスされたデータとファイルが一緒に格納されて、同じORMが使えて、そしてファイルが外部に保存される場合に不可能の外部キー制約ができることだ<sup>２</sup>）でもsqlarフォーマットを特別に作ったテーブルの代わりに使用するメリットはsqlite3のCLIのtarみたいなオプション（そしてこれ）が使えることだ。

私がこれを作る動機は、大きいデータセットと付属オーディオファイルを持って、導入時にそれぞれElasticsearchとS3にプッシュするけどその時までどこかの中間の場所で保存しないとならなかった。私がすでにEntity Frameworkを使ってデータをsqliteファイルに書き込んでいて、最初はオーディオファイルをフォルダーに保存していたけど、ファイル名がSHA1ハッシュだと考えると、このようにアクセスできるようにするのは特に意味がなかった。逆に、Windowsファイルパスや存在しないファイル等を扱うことをかかえた。ファイルをデータベースに移動するのは主キーがS3のオブジェクト名と同じもので、そのテーブルを外部キーによって参照してデータ整合性を強化できるという意味した。それにラップトップとデスクトップを切り替えるために一つのファイルをコピーしていいから楽になった<sup>３</sup>。

でも問題があった。開発環境ではローカルサーバーが本番のS3バケットではなくそのローカルのファイルに指してほしかった。別の開発バケットなんて不要だった。ディスク上のファイルだったら単純に静的ファイルとして提供できたけど、本番で要らないMicrosoft.Data.Sqliteへの依存関係を追加してDockerイメージを膨らましたくなかった。プリプロセッサ　ディレクティブは醜い。どうしよう？BLOBを静的ファイルとして提供するだけの別のコンテイナーをComposeファイルに追加して開発でS3の代わりにしようか！そしてNginxやApacheみたいなディレクトリリスト機能も入れって、必要のないのにシンボリックリンク対応を実装しよう！それでそれで……FTPサーバーを！なんていいアイデアだ！けして全く別のプロジェクトに膨大してしまわないでしょう！

とにかく、全く別のプロジェクトに膨大してしまったし・・・

> <sup>１</sup> そのテーブルが[ここ、Storageの下に](https://sqlite.org/sqlar/doc/trunk/README.md)もう少し詳しく説明される。タイムラインによると、これは2017年にsqlite3のCLIに統合された前の元のsqlarプロジェクトであるようだ。
>
> <sup>２</sup> それから.docxなどのように[ファイルフォーマットとしてアーカイブを使う](https://sqlite.org/appfileformat.html)用途もある。あの記事が少し偏ってると思うが、少なくとも[これより](https://ja.wikipedia.org/wiki/Open_Packaging_Conventions)sqliteの方がいいかと。
>
> <sup>３</sup> と言っても、何でもに相応しいわけではない。SQLiteのBLOBは1か2 GBの[サイズ制限](https://www.sqlite.org/limits.html)があるし、今のCLIが期待したほど堅牢じゃないかも（とくに-Cオプションがちゃんと動作しないという問題があった）

## オプション

サーバーはアーカイブのあるディレクトリが/srvにマウントされて、アーカイブへの相対パスが引数として渡される（イメージ名の後、またはcompose.yamlで`command:`に）と期待する。

次の表に、環境変数として渡せるオプションを示す：

|環&#8288;境&#8288;変&#8288;数|デ&#8288;フ&#8288;ォ&#8288;ル&#8288;ト&#8288;値&nbsp;&nbsp;|説&#8288;明|
|---|---|---|
|`TZ`|UTC|変更時刻を表示するためのタイムゾーン ([_List of tz database time zones_](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones)を参照<!-- English only -->)|
|`LANG`|en_US|数値等をフォーマットするためのロケール|
|`SizeFormat`|Binary|Bytes = ファイルサイズを書式なしでバイトで表示する<br />Binary = バイナリ単位を使う（KiB、MiB、GiB、TiB）<br />SI = SI単位を使う（KB、MB、GB、TB）|
|`DirectoriesFirst`|true|ディレクトリをファイルの前にグループする|
|`CaseInsensitive`|false|大文字小文字を区別しないファイルシステムとして扱う|
|`StaticSite`|false|ディレクトリリストを無効して、存在するとindex.htmlを提供して、見つけられない場合/404.htmlを提供する|
|`Charset`|utf-8|ファイルストリームのContent-Typeヘッダーの文字コードを設定する。無効にするには空の文字列に設定して。|
|`EnableFtp`|false|FTPサーバーを起動する|
|`FtpPasvPorts`|10000-10009|受動モードのためのポート範囲。ホストとコンテイナーのポートは一致する必要がある。ポート数が大きい場合はDockerが遅くなる可能性があるので、広い範囲は推奨しない。|
|`FtpPasvAddress`|127.0.0.1|FTPサーバーの外部IPアドレス|

> [!TIP]
> このようなエイリアスを.bashrc等に追加すると`sqlarserver foo.db`だけでサーバーを起動できるようになる：
> ```bash
> alias sqlarserver='docker run -it --rm -v .:/srv -p 3939:80 -e TZ=Asia/Tokyo ghcr.io/maxkagamine/sqlarserver'
> ```

## FTPサーバーの実行

![適切なポートを開いてEnableFtpをtrueに設定してサーバーを実行すると、WinSCPなどのFTPクライアントからサーバーに接続してsqliteアーカイブを参照できるようになる。](.github/images/browsing%20an%20sqlar%20in%20winscp%20over%20ftp.avif)

<sup>rin.avifイラストは<a href="https://twitter.com/Noartnolife1227/status/1531168810098917376">としたのあ (@Noartnolife1227)</a>による</sup>

Junkerの[C#のFTPサーバー](https://github.com/FubarDevelopment/FtpServer/)がファイルシステムの抽象化のインタフェースがあるのお陰で、私はバックエンドとしてsqlarserverの内部用のファイルツリーを使う実装を作成できた。これでSQLiteアーカイブの内容をFTP経由で参照できる！

![だがなぜ](.github/images/but%20why.avif)

¯\\\_(ツ)\_/¯

でも従来の圧縮ツール（7-Zipとか）でsqlarがサポートされてないため、これはWebインタフェースを使うと退屈になる場合でSQLiteデータベースからファイルを一括抽出するためのGUI代替として役に立つかもしれない。いずれにしても、FTPを有効にしてサーバーを実行する方法はこれだ：

```diff
 $ docker run -it --rm \
     -v .:/srv \
     -p 3939:80 \
+    -p 21:21 \
+    -p 10000-10009:10000-10009 \
+    -e EnableFtp=true \
     ghcr.io/maxkagamine/sqlarserver sqlite.sqlar
```

ポート21は何でもにマッピングできるけど、FTPプロトコルの一部はサーバーがデータ転送のためにどのIPとポートに接続すべきだとクライアントに伝えることなので、PASV<sup>１</sup>のポート範囲の10000-10009はホストとコンテイナーが一致する必要がある。`FtpPasvPorts`の設定で変更できる。もしサーバーがlocalhostで実行してなければ`FtpPasvAddress`をFTPクライアントに入力すると同じIPアドレスに設定する必要がある。

> <sup>１</sup> PASVとはFTPの受動モード（英：passive mode）に指して、サーバーがデータ転送のためにポートを開けてクライアントに接続するように指し示すことだ。（ポート21はコントロールで、コマンド通信のためでけに使われる。）その反対はアクティブモードで、ファイアウォールの前の時代🦕からだしサーバーが直接にクライアントへの接続を確立することだった。

> [!WARNING]
> Dockerはデフォルトで公開されたポートを0.0.0.0にバインドして外部からアクセスできるようにファイアウォール規則を作るのだ。信頼できないネットワークでは、またはマシンがインターネットに開けてる場合は、明示的にlocalhostにバインドする（例えば、`-p 127.0.0.1:21:21`）か[デフォルトのバインド・アドレスを変更](https://docs.docker.com/network/packet-filtering-firewalls/#setting-the-default-bind-address-for-containers)しないとならない。（DockerのLinux版を使用しているWSLユーザーはこれを気にする必要はないはずだ。）

## 静⁠的⁠サ⁠イ⁠ト⁠の⁠提⁠供（試験的）

`StaticSite`をtrueに設定すると、ディレクトリリストが無効されて、代わりにGitHub Pagesとかの静⁠的⁠サ⁠イ⁠トのホストのようにindex.htmlのファイルを提供して、見つけられないと/404.htmlを提供する。私のウェブサイトの以前のバージョンのアーカイブ（5.6 MiB）をダウンして試してみることができる：

```
$ wget https://gist.github.com/maxkagamine/f8fe0ca583a66ee99aa746362d34eda5/raw/kagamine.dev_2020-07-10.sqlar
$ docker run -it --rm -v .:/srv -p 3939:80 -e StaticSite=true ghcr.io/maxkagamine/sqlarserver kagamine.dev_2020-07-10.sqlar
```

実際にsqlarserverをこのために使う理由が分からないけどね。それにサーバーが`no-cache`を設定するからウェブサイトのためにかなり不味い（それが構成可能にするか`StaticSite`が有効の場合で無効することができるけど）でもコンセプトとして面白いと思う。現代のMHTML<sup>１</sup>の代替品のように。

さて、あなたが私のようであれば、きっとこう考えている：　sqlarのキーポイントはSQLiteデータベースの中のアーカイブであることだから、もしそのデータベースのテーブルのアクセスが可能にするREST APIを、例えば、/\_sqlarエンドポイントでサーバーに追加したと？それなら「サーバーレス」なデータベースとの完全なウェブサイトがすべて一つの自己完結型<sup>２</sup>のSQLiteファイルから実行できるじゃないか！サイト自体もデータベースに格納されてるので、興味深い結果はサイトが実行時にその自体を変更できるということだ (CMS?)。先のREST APIを同じくデータベースに格納されている呼び出し可能なWASM関数と置き換えれば、サーバーレス<sup>３</sup>クラウドスタックを単一ファイルから実行できる。

とにかく、それは読者の練習課題にしとく。私はもうこれに時間を掛けすぎたから(^\_^;)

> <sup>１</sup> 私はその「M」がMicrosoftの略で、ただの独自のZIPフォーマットだといつも思ったが、どうやらもっと悪い：簡単的に[でかいメールファイルだ](https://en.wikipedia.org/wiki/MHTML)。<!-- JA article is too brief -->
>
> <sup>２</sup> 中身を覗いてみて物事を実行しているDockerコンテイナーを見ない限りよね。でもインフラを無視するこそがサーバーレスじゃない？
>
> <sup>３</sup> ２をご参照。でもこれを[AOTコンパイル](https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/native-aot?view=aspnetcore-8.0)できるなら、SQLiteデータベースを実行可能に埋め込む方法があると思う…🤔

## ライセンス

[Apache 2.0](LICENSE.txt)
