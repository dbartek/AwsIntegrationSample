// Write your Javascript code.

function submitForm() {
    var name = $("#file-name").text();
    if (name != null && name.length > 0) {
        $("#key").val(name);
        $("#sendPictureForm").submit();
    }
}

function requestTransform() {
    var pictures = [];
    $('#pictures input:checked').each(function () {
        pictures.push($(this).closest(".js-table-row").data("name"));
    });
    if (pictures.length == 0) {
        alert("Nie wybrano zadnego obrazka");
        return;
    }
    $.ajax({
        method: "POST",
        url: "/Home/Transform",
        data: { fileNames: pictures, transformation: $("#transform").val() },
        success: function () {
            alert("Zlecono transformacje wybranych obrazkow");
        },
        error: function (e) {
            alert(e);
        }
    });
}