mergeInto(LibraryManager.library, {
    StageMakerSelectJsonFile: function(receiverPtr, successPtr, errorPtr) {
        var receiver = UTF8ToString(receiverPtr);
        var successMethod = UTF8ToString(successPtr);
        var errorMethod = UTF8ToString(errorPtr);

        var input = document.createElement("input");
        input.type = "file";
        input.accept = ".json,application/json";
        input.style.display = "none";

        input.onchange = function() {
            var file = input.files && input.files.length > 0 ? input.files[0] : null;
            document.body.removeChild(input);

            if (!file) {
                SendMessage(receiver, errorMethod, "");
                return;
            }

            var reader = new FileReader();
            reader.onload = function(event) {
                SendMessage(receiver, successMethod, event.target.result || "");
            };
            reader.onerror = function() {
                SendMessage(receiver, errorMethod, "Import failed: could not read file.");
            };
            reader.readAsText(file);
        };

        document.body.appendChild(input);
        input.click();
    }
});
