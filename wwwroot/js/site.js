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
function toggleFavorite(gameId, element) {
    $.ajax({
        type: 'POST',
        url: '/Games/togglefavorite/' + gameId,
        success: function (response) {
            let icon = element.querySelector("i");
            if (icon.classList.contains("fa-solid")) {
                icon.classList.remove("fa-solid");
                icon.classList.add("fa-regular");
            } else {
                icon.classList.remove("fa-regular");
                icon.classList.add("fa-solid");
            }
        },
        error: function () {
            alert("Error on changing the favorite status of the game. Reload and try again!");
        }
    });
}

// function for making a game offical or removing it - ajax
function toggleOfficial(gameId, element) {
    $.ajax({
        type: 'POST',
        url: '/a/toggleofficial/' + gameId,
        success: function () {
            let text = element.innerText;
            if (text == "Change to official status") {
                element.innerText = "Remove official status";
            } else {
                element.innerText = "Change to official status";
            }
            location.reload();
        },
        error: function () {
            alert("Error on changing the official status of the game. Reload and try again!");
        }
    });
}

// ajax functions for following and unfollowing
function follow(id, element) {
    let url = '/follower/follow/' + id;
    $.ajax({
        type: 'POST',
        url: url,
        success: function () {
            element.innerText = "Unfollow";
            location.reload();
        },
        error: function () {
            alert("Error on following the user, Reload and try again!")
        }
    })
}

function unfollow(id, element) {
    let url = '/follower/unfollow/' + id;
    $.ajax({
        type: 'POST',
        url: url,
        success: function () {
            element.innerText = "Follow";
            location.reload();
        },
        error: function () {
            alert("Error on unfollowing the user, Reload and try again!")
        }
    })
}

