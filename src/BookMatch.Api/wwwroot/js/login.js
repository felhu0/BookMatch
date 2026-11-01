// Logik för inloggningssidan: skickar formulären och går tillbaka till chatten vid lyckad inloggning

import { login, register } from "./api.js";

const loginForm = document.getElementById("login-form");
const registerForm = document.getElementById("register-form");
const status = document.getElementById("status");

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitAccountForm(loginForm, login);
});

registerForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    await submitAccountForm(registerForm, register);
});

// Skickar e-post och lösenord med vald funktion (login eller register)
async function submitAccountForm(form, action) {
    const formData = new FormData(form);
    const button = form.querySelector("button");

    // Förhindra dubbelklick (varje felaktigt försök räknas mot lockout)
    button.disabled = true;
    setStatus("");

    try {
        await action(formData.get("email"), formData.get("password"));
        // Tillbaka till chatten som inloggad
        window.location.href = "/";
    } catch (error) {
        setStatus(error.message);
        // Töm lösenordsfältet vid fel, e-posten får vara kvar
        form.password.value = "";
        form.password.focus();
    } finally {
        button.disabled = false;
    }
}

function setStatus(text) {
    status.textContent = text;
}