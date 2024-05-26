<h1 align="center">
  <img src=".github/images/icon.avif" width="64" aria-hidden="true" />
  <br />
  SQLite Archive Server
</h1>

<p align="center">
  <a href="#quick-demo">Quick&nbsp;demo</a> ・
  <a href="#overview--motivation">Overview&nbsp;/&nbsp;Motivation</a> ・
  <a href="#options">Options</a> ・
  <a href="#running-the-ftp-server">Running&nbsp;the&nbsp;FTP&nbsp;server</a> ・
  <a href="#serving-a-static-site-experimental">Serving&nbsp;a&nbsp;static&nbsp;site</a> ・
  <a href="">日&#8288;本&#8288;語</a>
</p>

<picture>
  <source media="(prefers-color-scheme: dark)" srcset=".github/images/screenshot-dark.avif" />
  <img src=".github/images/screenshot-light.avif" alt="Opening the server in a browser presents the archive contents as a directory listing, similar to a standard web server like Nginx or Apache." />
</picture>

## Quick demo

Download the SQLite source ~~tarball~~ sqlarball and open it in a web browser:

```
$ wget https://www.sqlite.org/src/sqlar/sqlite.sqlar
$ docker run -it --rm -v .:/srv -p 3939:80 sqlarserver sqlite.sqlar
$ open http://localhost:3939
```

## Overview / Motivation

An [SQLite Archive](https://sqlite.org/sqlar.html), or _sqlar_ for short (probably pronounced /ˈɛs kjuː&#8202;ˈlɑːr/, but I sometimes pronounce it /sklɑːr/ because "sqlarball" sounds funnier that way), is simply an SQLite database that uses a standard table schema¹ for storing files as blobs. The main page goes over some reasons for doing this (the big ones IMO being that your relational/indexed data and files are kept together, use the same ORM, and can have foreign key constraints that simply aren't possible when files are stored externally²), but the advantage of using the sqlar format over an ad hoc table is that it enables use of the sqlite3 CLI's tar-like options &mdash; and now this as well.

My motivation for making this was I had a large dataset and accompanying audio files, where upon deployment these would be pushed to Elasticsearch and S3 respectively, but until then needed to be stored in some intermediate location. I was already using Entity Framework and dumping the data into an sqlite file; at first, I was saving the audio files to a folder, but given that their filenames were SHA1 hashes, there wasn't really any meaning in having them accessible in this way. Instead, it burdened me with having to deal with Windows file paths, potential for missing files, and so on. Moving them into the sqlite file itself meant that the primary key was literally the S3 object name, and I could reference this table via a foreign key to enforce data integrity. Switching between my laptop and desktop is easier, too, as I can just copy the file over.³

But this provided a challenge: for development, I wanted my local server to point to the local audio files, not the production S3 bucket. A separate dev bucket would be overkill. Had these been files on disk, I could simply serve them as static files, but I didn't want to add Microsoft.Data.Sqlite as a dependency and inflate the docker image when it wouldn't need that in production. Preprocessor directives are ugly. What to do? Why not add a separate container to the compose file that just serves the blobs as static files, and use that as a stand-in for S3 in dev! And then, let's add directory listing, like Nginx or Apache! And symlink support, too, even though I wouldn't need it! And, and... _an FTP server!_ What a great idea! Surely this won't balloon into a whole project!

So anyway, this ballooned into a whole project...

> ¹ The table is described in slightly more detail [here](https://sqlite.org/sqlar/doc/trunk/README.md), under Storage. Based on the timeline, this appears to be the original sqlar project before it was added to the sqlite3 CLI in 2017.
> 
> ² And then there's the usecase of [archives as file formats](https://sqlite.org/appfileformat.html), like .docx and similar. Their article feels a bit biased, but I would say I'd prefer sqlite over [this clunker](https://en.wikipedia.org/wiki/Open_Packaging_Conventions).
> 
> ³ All this being said, it's not suitable for everything: SQLite blobs have a 1 or 2 GB [size limit](https://www.sqlite.org/limits.html), and the CLI isn't currently as robust as I'd like it to be (I found trouble with the -C option not working, in particular).

## Options

The server expects the directory containing the archive to be mounted at /srv, and the path to the archive relative to that directory passed as an argument (after the image name, or as `command:` in compose.yaml).

The following options can be passed as environment variables:

|Environment&nbsp;variable|Default&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;|Description|
|---|---|---|
|`TZ`|UTC|Timezone for displaying date modified (see [_List of tz database time zones_](https://en.wikipedia.org/wiki/List_of_tz_database_time_zones))|
|`LANG`|en_US|Locale used for formatting numbers etc.|
|`SizeFormat`|Binary|Bytes = Display file sizes in bytes without formatting<br />Binary = Use binary units (KiB, MiB, GiB, TiB)<br />SI = Use SI units (KB, MB, GB, TB)|
|`DirectoriesFirst`|true|Group directories before files|
|`CaseInsensitive`|false|Treat the archive as a case-insensitive filesystem|
|`StaticSite`|false|Disable directory listing and serve index.html files where present and /404.html when not found|
|`Charset`|utf-8|Sets the charset in the Content-Type header of file streams. Empty string to disable.|
|`EnableFtp`|false|Start the FTP server|
|`FtpPasvPorts`|10000-10009|Port range used for passive mode. Host and container ports must match. Avoid too large a range, as many ports can make docker slow.|
|`FtpPasvAddress`|127.0.0.1|The FTP server's external IP address|

> [!TIP]
> Add an alias to your .bashrc or similar so you can just do `sqlarserver foo.db` to bring up the server, like so:
> ```bash
> alias sqlarserver='docker run -it --rm -v .:/srv -p 3939:80 -e TZ=America/Los_Angeles sqlarserver'
> ```

## Running the FTP server

![Running the server with the appropriate ports and EnableFtp set to true allows for connecting to the server and browsing the sqlite archive via an FTP client such as WinSCP.](.github/images/browsing%20an%20sqlar%20in%20winscp%20over%20ftp.avif)

<sup>rin.avif artwork by <a href="https://twitter.com/Noartnolife1227/status/1531168810098917376">としたのあ (@Noartnolife1227)</a></i></sup>

Thanks to Junker's [C# FTP server](https://github.com/FubarDevelopment/FtpServer/) having an abstracted file system interface, I was able to write an implementation that uses sqlarserver's internal file node tree as a backend. Now you can browse the contents of an SQLite Archive via FTP!

![But why?](.github/images/but%20why.avif)

¯\\\_(ツ)\_/¯

Although, in the absence of support for sqlar in traditional archive tools (7-Zip etc.), I could see this being useful as a GUI option for bulk-extracting files from an SQLite database, which may be tedious through the web interface. In any case, here's how to run the server with FTP enabled:

```diff
 $ docker run -it --rm \
     -v .:/srv \
     -p 3939:80 \
+    -p 21:21 \
+    -p 10000-10009:10000-10009 \
+    -e EnableFtp=true \
     sqlarserver sqlite.sqlar
```

Port 21 can be mapped to whatever, but the PASV¹ port range, 10000-10009, needs to be the same on both the host and container since part of the FTP protocol involves the server telling the client what IP and port to connect to for data transfer. You can change it by setting `FtpPasvPorts`. If the server's not running on localhost, you'll need to set `FtpPasvAddress` to whatever IP address you put in your FTP client.

> ¹ PASV refers to FTP's _passive mode_, where the server opens a port for data transfer and tells the client to connect to it (port 21 is the control connection, used only for sending commands). The opposite, _active mode_, is from The Time Before Firewalls&#8288;🦕 and entailed the server directly initiating a connection back to the client.

> [!WARNING]
> Docker binds published ports to 0.0.0.0 by default and creates firewall rules to make them accessible externally. If you're on an untrusted network or your machine is open to the Internet, you should bind to localhost explicitly (e.g. `-p 127.0.0.1:21:21`) or [change the default bind address](https://docs.docker.com/network/packet-filtering-firewalls/#setting-the-default-bind-address-for-containers). (WSL users running the Linux version of Docker shouldn't need to worry about this.)

## Serving a static site (experimental)

If you set `StaticSite` to true, it'll disable directory listings and instead serve index.html files, or /404.html if not found, similar to GitHub Pages and other static site hosts. You can download an archive of an old version of my website (5.6 MiB) to try this out:

```
$ wget https://gist.github.com/maxkagamine/f8fe0ca583a66ee99aa746362d34eda5/raw/kagamine.dev_2020-07-10.sqlar
$ docker run -it --rm -v .:/srv -p 3939:80 -e StaticSite=true sqlarserver kagamine.dev_2020-07-10.sqlar
```

I don't know why you'd _actually_ do this; in fact the server sets `no-cache` which makes it pretty bad for a website (perhaps that could be made configurable or disabled when `StaticSite` is active, though). But it's an interesting concept, like a modern alternative to MHTML¹.

Now if you're like me, you're probably thinking: the key point of sqlar is that it's an archive _inside_ an SQLite database, so what if you were to add, say, an /\_sqlar endpoint to the server that enabled REST API access to the DB's tables? Then you could run an _entire_ website, complete with a "serverless" database, all from a single, self-contained² SQLite file! Since the website itself is stored in the database, an interesting consequence of this is that the site could modify itself at runtime (a CMS?). Replace the REST API with callable WASM functions, also stored in the archive, and you could run a whole serverless³ cloud stack out of a single file.

Anyway, I'll leave that as an exercise for the reader. I've spent too much time on this already :)

> ¹ I always thought the "M" stood for Microsoft and that it was just a proprietary zip format, but it turns out it's much worse than that: it's basically [a gigantic email file](https://en.wikipedia.org/wiki/MHTML).
>
> ² As long as you don't look under the hood at the big docker container running the show, that is. But isn't ignoring the infrastructure what "serverless" is all about?
>
> ³ See #2. Then again, if you could get this to [AOT-compile](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/native-aot?view=aspnetcore-8.0), I do believe it's possible to embed an SQLite database within an executable...🤔

## License

[Apache 2.0](LICENSE.txt)
