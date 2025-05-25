// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// function for showing the register error
window.onload = function () {
    var registerError = document.getElementById("registerError");
    if (registerError && registerError.innerText.trim() !== "") {
        registerError.style.display = "block";
    } else if (registerError) {
        registerError.style.display = "none";
    }

    // function for showing the login error
    var loginError = document.getElementById("loginError");
    if (loginError && loginError.innerText.trim() !== "") {
        loginError.style.display = "block";
    } else if (loginError) {
        loginError.style.display = "none";
    }
};

// function for updating the list of liked games - ajax


