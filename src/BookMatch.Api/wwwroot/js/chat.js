import { streamChat } from "./api.js";

// Kopplar ihop formuläret och följdfrågorna med API:t och visar meddelanden i chattvyn – alltid som text, aldrig som HTML

// Samma gränser som ChatRequestValidator i backend
const MAX_HISTORY_COUNT = 20;
const MAX_ASSISTANT_MESSAGE_LENGTH = 5000;

const startForm = document.getElementById("start-form");
const chat = document.getElementById("chat");
const messages = document.getElementById("messages");
const status = document.getElementById("status");
const followUpForm = document.getElementById("follow-up-form");
const followUpInput = document.getElementById("follow-up");
const followUpButton = followUpForm.querySelector("button");

// Formulärets val (skickas med varje anrop, servern bygger om första meddelandet från dem)
let choices = null;

// Historik (tidigare svar och följdfrågor), finns bara i webbläsarens minne under besöket
let history = [];

// Första meddelandet: formulärets val
startForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const formData = new FormData(startForm);
    choices = {
        language: formData.get("language"),
        genre: formData.get("genre"),
        level: formData.get("level")
    };
    history = [];

    addMessage("user", describeChoices());
    startForm.hidden = true;
    chat.hidden = false;

    const reply = await sendAndShow(choices);

    if (reply === null) {
        // Visa formuläret igen så användaren kan försöka på nytt
        startForm.hidden = false;
        return;
    }

    // AI:ns första svar blir början på historiken
    addToHistory("Assistant", reply);
    setFollowUpEnabled(true);
});

// Följdfrågor: fritext från användaren
followUpForm.addEventListener("submit", async (event) => {
    event.preventDefault();

    const message = followUpInput.value.trim();
    if (message === "") {
        return;
    }

    // Förhindra dubbelklick medan svaret strömmar
    setFollowUpEnabled(false);
    followUpInput.value = "";
    trimHistory();

    const userItem = addMessage("user", message);
    const reply = await sendAndShow({ ...choices, history, message });

    if (reply === null) {
        // Ta bort frågan och lägg tillbaka texten så användaren kan försöka igen
        userItem.remove();
        followUpInput.value = message;
    } else {
        addToHistory("User", message);
        addToHistory("Assistant", reply);
    }

    setFollowUpEnabled(true);
    followUpInput.focus();
});

// Skickar anropet, strömmar in svaret och returnerar hela svarstexten (null vid fel)
async function sendAndShow(request) {
    setStatus("Hämtar svar...");
    const replyItem = addMessage("assistant", "");

    try {
        await streamChat(request, (text) => {
            replyItem.textContent += text;
            replyItem.scrollIntoView({ block: "end" });
        });
        setStatus("");
        return replyItem.textContent;
    } catch (error) {
        setStatus(error.message);
        // Ofullständiga svar sparas inte, de tas bort från chattvyn
        replyItem.remove();
        return null;
    }
}

// Lägger till ett meddelande i historiken, AI-svar kortas till serverns maxlängd
function addToHistory(role, content) {
    const text = role === "Assistant" ? content.slice(0, MAX_ASSISTANT_MESSAGE_LENGTH) : content;
    history.push({ role, content: text });
}

// Håller historiken inom serverns maxantal: tar bort äldsta fråga + svar, men behåller boktipsen (första svaret)
function trimHistory() {
    while (history.length > MAX_HISTORY_COUNT - 1) {
        history.splice(1, 2);
    }
}

// Lägger till ett meddelande i chattvyn och returnerar det
function addMessage(role, text) {
    const item = document.createElement("li");
    item.className = `message message-${role}`;
    item.dir = "auto"; // textriktning efter innehållet (arabiska höger till vänster)
    item.textContent = text; // XSS-skydd (Cross-Site Scripting): visas som text, aldrig som HTML
    messages.append(item);
    item.scrollIntoView({ behavior: "smooth", block: "end" });
    return item;
}

// Beskriver valen med de svenska texterna från dropdown-menyerna
function describeChoices() {
    const language = startForm.language.selectedOptions[0].text;
    const genre = startForm.genre.selectedOptions[0].text;
    const level = startForm.level.selectedOptions[0].text;
    return `${language} · ${genre} · ${level}`;
}

// Aktiverar eller inaktiverar fältet och knappen för följdfrågor
function setFollowUpEnabled(enabled) {
    followUpInput.disabled = !enabled;
    followUpButton.disabled = !enabled;
}

function setStatus(text) {
    status.textContent = text;
}