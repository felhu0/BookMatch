// Visar inloggningsstatus i kontoraden och hanterar utloggning

import { getCurrentUser, logout } from "./api.js";

const loginLink = document.getElementById("login-link");
const accountEmail = document.getElementById("account-email");
const logoutButton = document.getElementById("logout-button");

// Kontrollera inloggning när sidan laddas
showAccount(await getCurrentUser());

logoutButton.addEventListener("click", async () => {
    try {
        await logout();
    } catch (error) {
        console.error(error);
    }
    // Ladda om sidan så att chatt och historik i minnet töms
    window.location.reload();
});

// Visar inloggad användare eller länk till inloggning
function showAccount(email) {
    const loggedIn = email !== null;

    loginLink.hidden = loggedIn;
    accountEmail.hidden = !loggedIn;
    logoutButton.hidden = !loggedIn;
    accountEmail.textContent = loggedIn ? `Inloggad som ${email}` : "";
}