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
};

// function for showing the login error
window.onload = function () {
    var loginError = document.getElementById("loginError");
    if (loginError && loginError.innerText.trim() !== "") {
        loginError.style.display = "block";
    } else if (loginError) {
        loginError.style.display = "none";
    }
};


// function for updating the list of liked games
function toggleFavorite(gameId, element) {
    fetch(`/Games/ToggleFavorite/${gameId}`, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(response => {
            if (response.ok) {
                let icon = element.querySelector("i");
                if (icon.classList.contains("fa-solid")) {
                    icon.classList.remove("fa-solid");
                    icon.classList.add("fa-regular");
                } else {
                    icon.classList.remove("fa-regular");
                    icon.classList.add("fa-solid");
                }
            } else {
                alert("Eroare la schimbarea statusului de favorite.");
            }
        })
        .catch(error => console.error("Eroare:", error));
}

// function for making a game offical or removing it
function toggleOfficial(gameId, element) {
    fetch(`/a/ToggleOfficial/${gameId}`, {
        method: 'POST',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(response => {
            if (response.ok) {
                let text = element.text;
                if (text == "Change to official status") {
                    element.innerText = "Remove official status";
                } else {
                    element.innerText = "Change to official status";
                }
                location.reload();
            } else {
                alert("Eroare la schimbarea statusului oficial");
            }
        })
        .catch(error => console.error("Eroare:", error));
}



