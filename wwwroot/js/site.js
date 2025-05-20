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

// function for joining a room
function join(roomId) {
    $.ajax({
        type: 'POST',
        url: "/Room/Join/" + roomId,
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert("There was an error trying to join the room");
        }
    });
}

// function for leaving a room
function leave(roomId) {
    $.ajax({
        type: 'POST',
        url: "/Room/Leave/" + roomId,
        success: function (response) {
            if (response.success) {
                location.reload();
            } else {
                alert(response.message);
            }
        },
        error: function () {
            alert("There was an error trying to leave the room");
        }
    });
}

// global variables
let currentMode = 'single';
let teamCount = 0;

function checkAndOpenModal(roomId) {
    if (currentMode === 'team') {
        $('#startEarlyModal').modal('show');
    } else {
        if (confirm('Are you sure you want to start the match in single mode?')) {
            startSingleGame(roomId);
        }
    }
}
// toggling room mode
function toggleMode(mode) {
    currentMode = mode;
}
// function for adding a team
function addTeam() {
    teamCount++;
    const teamHtml = `
        <div class="col-md-4 mb-3 team-card" data-team-id="${teamCount}">
            <div class="card">
                <div class="card-body">
                    <div class="d-flex justify-content-between">
                        <h5 class="card-title">Team ${teamCount}</h5>
                        <button type="button" class="btn-close" onclick="removeTeam(${teamCount})"></button>
                    </div>
                    <div class="mb-3">
                        <label class="form-label">Team Name</label>
                        <input type="text" class="form-control team-name" 
                               value="Team ${teamCount}" 
                               onchange="updateTeamOptions(${teamCount}, this.value)" required title="The team must have a name">
                    </div>
                </div>
            </div>
        </div>
    `;

    $('#teamsContainer').append(teamHtml);
    updateAllTeamSelects();
}
// function for removing a team
function removeTeam(teamId) {
    $(`.team-card[data-team-id="${teamId}"]`).remove();
    updateAllTeamSelects();
}
// function for dynamically updating teams
function updateAllTeamSelects() {
    $('.team-select').each(function () {
        const defaultOption = $(this).find('option:first');
        $(this).empty().append(defaultOption);
    });

    $('.team-card').each(function () {
        const teamId = $(this).data('team-id');
        const teamName = $(this).find('.team-name').val();

        $('.team-select').each(function () {
            $(this).append(`<option value="${teamName}">${teamName}</option>`);
        });
    });
}

function updateTeamOptions(teamId, newName) {
    updateAllTeamSelects();
}
// starting single game
function startSingleGame(roomId) {
    $.ajax({
        url: '/Room/StartIndividual',
        type: 'POST',
        data: { roomId: roomId },
        success: function () {
            location.reload();
        },
        error: function () {
            toastr.error('Failed to start the match.');
        }
    });
}
// starting team game
function startGame(roomId) {
    const teamAssignments = {};
    const roles = {};

    $('select[name^="teamAssignments"]').each(function () {
        const playerId = $(this).attr('name').match(/\[(\d+)\]/)[1];
        teamAssignments[playerId] = $(this).val();
    });

    $('input[name^="roles"]').each(function () {
        const playerId = $(this).attr('name').match(/\[(\d+)\]/)[1];
        roles[playerId] = $(this).val();
    });
    $.ajax({
        url: '/Room/Start',
        type: 'POST',
        data: {
            roomId: roomId,
            teamAssignments: teamAssignments,
            roles: roles
        },
        success: function () {
            location.reload();
        },
        error: function () {
            toastr.error('Failed to start the match.');
        }
    });
}

