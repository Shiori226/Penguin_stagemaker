mergeInto(LibraryManager.library, {
    // 複数のファイルをZip圧縮してダウンロードする関数
    DownloadZip: function(zipFileNamePtr, filePathsPtr, fileContentsPtr, fileSizesPtr, fileCount) {
        // C#から渡されたポインタをJavaScriptの変数に変換
        var zipFileName = UTF8ToString(zipFileNamePtr);
        var paths = UTF8ToString(filePathsPtr).split('|');
        
        // --- 1. JSZipライブラリを動的にロード ---
        var script = document.createElement('script');
        script.src = "StreamingAssets/JSZip/jszip.min.js"; // StreamingAssets内のライブラリを指定
        document.body.appendChild(script);

        script.onload = function() {
            // --- 2. ライブラリのロード完了後にZip処理を実行 ---
            var zip = new JSZip();

            var offset = 0;
            for (var i = 0; i < fileCount; i++) {
                var path = paths[i];
                var size = HEAP32[fileSizesPtr / 4 + i]; // int配列としてサイズを取得

                if (path && size > 0) {
                    // C#から渡された巨大なバイト配列から、ファイル一つ分のデータを切り出す
                    var content = new Uint8Array(HEAPU8.buffer, fileContentsPtr + offset, size);
                    // Zipにファイルを追加
                    zip.file(path, content);
                    offset += size;
                }
            }
            
            // --- 3. Zipファイルを生成し、ダウンロードを開始 ---
            zip.generateAsync({ type: "blob", compression: "DEFLATE" }).then(function(content) {
                var url = URL.createObjectURL(content);
                var a = document.createElement("a");
                a.href = url;
                a.download = zipFileName;
                document.body.appendChild(a);
                a.click();

                // 後処理
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            });

            document.body.removeChild(script);
        };
    },
});