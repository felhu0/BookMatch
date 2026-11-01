// Visar inloggningsstatus i kontoraden och hanterar utloggning

import { getCurrentUser, logout, deleteHistory } from "./api.js";

const loginLink = document.getElementById("login-link");
const accountEmail = document.getElementById("account-email");
const logoutButton = document.getElementById("logout-button");
const deleteHistoryButton = document.getElementById("delete-history-button");

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

// Radera sparad chatt (bara för inloggade)
deleteHistoryButton.addEventListener("click", async () => {
    // Bekräftelse, raderingen går inte att ångra
    if (!confirm("Vill du radera din sparade chatt? Det går inte att ångra.")) {
        return;
    }

    try {
        await deleteHistory();
        // Ladda om sidan så att chatten i minnet också töms
        window.location.reload();
    } catch (error) {
        alert(error.message);
    }
});

// Visar inloggad användare eller länk till inloggning
function showAccount(email) {
    const loggedIn = email !== null;

    loginLink.hidden = loggedIn;
    accountEmail.hidden = !loggedIn;
    logoutButton.hidden = !loggedIn;
    accountEmail.textContent = loggedIn ? `Inloggad som ${email}` : "";
    deleteHistoryButton.hidden = !loggedIn;
}