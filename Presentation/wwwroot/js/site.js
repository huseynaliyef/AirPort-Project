// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
$(document).ready(function () {
    showInputBox();
});
function showInputBox() {
    var selectedOption = $("#optionBox").val();

    // You can use a switch statement for different options if needed
    if (selectedOption == "TempDelta") {
        $("#endEDBox").show();
    } else {
        $("#endEDBox").hide();
    }
}

// Write your JavaScript code.