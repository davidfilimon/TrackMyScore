# TrackMyScore - Web Application

Welcome to the **TrackMyScore** web application. This project provides a modern solution for managing multiplayer board games, including user profiles, game rooms, tournaments, and performance statistics.

---

## 🏠 Home Page

Authenticated users are greeted with a welcome message and the latest updates from the platform.

![Home Page](poze/pagina-principala.png)

---

## 📅 Registration

New users can register through a dedicated form requiring unique credentials (username, email) and secure passwords (minimum 8 characters, match confirmation).

Passwords are encrypted using **PBKDF2** with:

* Iterations (for brute-force resistance)
* Random salt (ensuring unique hashes)
* Secure storage of all encryption components

![Register](poze/pagina-register.png)
![Encryption](poze/criptare.png)

---

## 🔐 Login

Users can log in with valid credentials. "Remember Me" support is provided via browser cookies. Password validation uses the same cryptographic process as during registration.

![Login](poze/pagina-login.png)

---

## ⟳ Password Reset

Forgot your password? Submit your email to receive a randomly generated 8-character temporary password.

![Reset Password](poze/pagina-resetare-parola.png)
![Reset Mail](poze/mail.png)

---

## 🔍 Search Page

A top navigation bar includes a search bar to find other user profiles. Minimum 1 character is required for suggestions based on username matches.

![Search](poze/pagina-cautare.png)

---

## 👤 Player Profile

The profile page displays user details, match/tournament history, stats, followers/following, and added games. Users can edit their own profile or follow/unfollow others.

![Profile](poze/pagina-profil.png)
![Followers Modal](poze/modal-followers.png)
![Edit Profile](poze/edit-profile.png)
![Another User Profile](poze/pagina-alt-profil.png)

---

## 🎮 Games Page

Three categorized game lists are displayed:

1. Personalized Recommendations (based on favorite similarity with 70%+ overlap)
2. Official Games (admin added)
3. User-Added Games

Users can:

* Add new games
* View detailed game info
* Admins can approve/remove games or make them official

![Games](poze/lista-jocuri.png)
![Recommendation Algorithm](poze/recommendation-algorithm.png)
![Add Game](poze/creare-joc.png)
![Admin Game Details](poze/detalii-joc.png)
![User Game Details](poze/detalii-joc-1.png)
![Games from Profile](poze/profil-jocuri.png)

---

## 🏋️‍♂️ Matches

View available and joined matches. Create a match by choosing type (Single or Team), configure players and settings.

### Match States:

* -1: Waiting
* 0: Active
* -2: Finished

![Matches Page](poze/pagina-meciuri.png)
![Create Match](poze/creare-meci.png)
![Team Room](poze/camera-echipe.png)
![Join Room](poze/join-team.png)
![Team Init](poze/initializare-echipe.png)
![Temp Team](poze/echipa-temporara.png)
![Single Match Wait](poze/meci-single.png)
![Single Match Active](poze/single-inceput.png)
![Team Match Active](poze/activ-team.png)
![Updated Match List](poze/meciuri-disponibile.png)
![Finished Team Match](poze/echipe-finalizat.png)
![Finished Single Match](poze/finalizat-single.png)
![Match History](poze/istoric-meciuri.png)
![Match Stats](poze/meciuri-statistici.png)

---

## 🏆 Tournaments

Players can join tournaments using a unique 6-character code. Tournaments can be "Single" or "Team" and host 2 to 16 starting matches.

Admins or hosts:

* Configure participants
* Monitor stages
* Advance winners
* Finalize results

![Tournaments](poze/lista-turnee.png)
![Create Tournament](poze/creare-turneu.png)
![Code Generator](poze/cod-turneu.png)
![Tournament Algorithm](poze/algoritm-creare-turneu.png)
![Details & Join](poze/detalii-turneu.png)
![Team Join Form](poze/intrare-echipe.png)
![Team Host View](poze/intrare-team.png)
![Active Single](poze/turneu-individual.png)
![Active Team](poze/turneu-team-activ.png)
![Bracket Stage](poze/bracket-individual.png)
![Final Stage](poze/etapa-finala-individuala.png)
![Single Ended](poze/turneu-single-finalizat.png)
![Team Ended](poze/turneu-team-finalizat.png)
![Tournament Profile View](poze/profil-turnee.png)
![Tournament Stats](poze/statistici-turnee.png)

### 🧬 Respect Points System

* All players start with 1 point
* Advancing to the next stage: double points
* Eliminated players receive their current points
* Winner gets the accumulated points

![Respect Points Algorithm](poze/algoritm-respect.png)

---

## 👷‍♂️ Admin Panel

Admins can:

* View app statistics
* Manage official games
* Monitor users and matches

![Admin Page](poze/pagina-admin.png)

---

## 🏁 Summary

This web app makes it easy to manage player profiles, games, matches, and tournaments, all while keeping accounts secure and adding fun features like following other players and earning respect points. It's a great fit for both casual board game fans and competitive communities.
