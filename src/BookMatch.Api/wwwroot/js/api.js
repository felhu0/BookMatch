// Skickar formulärets val till chatt-endpointen och tar emot svaret som SSE-ström (Server-Sent Events)
// onText anropas för varje textbit som kommer

// CSRF-token (Cross-Site Request Forgery), sparas bara i minnet
let csrfToken = null;

// Hämtar ny CSRF-token från servern, bara när token måste bytas, alltså efter inloggning och utloggning
export async function refreshCsrfToken() {
    let response;

    try {
        response = await fetch("/api/antiforgery/token");
    } catch (error) {
        console.error(error);
        throw new Error("Kunde inte nå servern. Kontrollera din anslutning.");
    }

    if (!response.ok) {
        throw new Error("Kunde inte hämta säkerhetstoken. Ladda om sidan.");
    }

    const data = await response.json();

    // Request-token från servern sparas som csrfToken (skickas i X-CSRF-TOKEN)
    csrfToken = data.requestToken;
}

// I vanliga anrop. Snabb, eftersom den återanvänder sparad token
export async function getCsrfToken() {
    if (csrfToken === null) {
        await refreshCsrfToken();
    }
    return csrfToken;
}

export async function streamChat(request, onText) {
    // Hämta CSRF-token (Cross-Site Request Forgery), sparad i minnet eller ny från servern
    const token = await getCsrfToken();
    let response;

    try {
        response = await fetch("/api/chat", {
            method: "POST",
            headers: {
                "Content-Type": "application/json", //skickar JSON
                "Accept": "text/event-stream",  //förväntar en SSE-ström tillbaka
                "X-CSRF-TOKEN": token  // request-token, jämförs med antiforgery-cookien på servern
            },
            body: JSON.stringify(request)
        });
    } catch (error) {
        //Nätverksfel (servern nås inte alls)
        console.error(error);
        throw new Error("Kunde inte nå servern. Kontrollera din anslutning.")
    }

    // Fel före streaming (t.ex. 400 vid validering) kommer som vanlig statuskod
    if (!response.ok) {
        throw new Error(getErrorMessage(response.status));
    }

    const reader = response.body.getReader();   // läser svaret bit för bit
    const decoder = new TextDecoder();          // gör om bytes till text (UTF-8)
    let buffer = "";                            // samlar text tills en hel händelse kommit

    //Läs bitarna i en loop
    while (true) {
        const { value, done } = await reader.read(); //väntar på nästa bit från servern
        if (done) {                                  //done blir true när servern stängt strömmen
            break;
        }

        // Bytes → text (UTF-8); stream: true sparar ett halvt tecken till nästa bit (å, ä, ö tar 2 bytes och kan delas av nätverket)
        buffer += decoder.decode(value, { stream: true });

        // En SSE-händelse (Server-Sent Events) avslutas med en tom rad ("\n\n")
        let separatorIndex;
        while ((separatorIndex = buffer.indexOf("\n\n")) !== -1) {
            const rawEvent = buffer.slice(0, separatorIndex);  // en hel händelse
            buffer = buffer.slice(separatorIndex + 2);         // resten sparas till nästa varv

            const { name, data } = parseEvent(rawEvent);

            if (name === "message") {
                onText(data.text);                              // visa textbiten
            } else if (name === "error") {
                throw new Error(getStreamErrorMessage(data.code));
            } else if (name === "done") {
                return;                                         // svaret är klart
            }
        }
    }
}

// Delar upp en SSE-händelse (Server-Sent Events) i namn och data
function parseEvent(rawEvent) {
    let name = "message";
    let data = "";

    for (const line of rawEvent.split("\n")) {
        if (line.startsWith("event: ")) {
            name = line.slice("event: ".length);
        } else if (line.startsWith("data: ")) {
            data += line.slice("data: ".length);
        }
    }

    return { name, data: JSON.parse(data) };
}

// Egna felmeddelanden per statuskod, serverns feltext visas aldrig direkt
function getErrorMessage(status) {
    switch (status) {
        case 400: return "Ogiltiga val. Ladda om sidan och försök igen.";
        case 429: return "Du har nått gränsen för antal frågor. Försök igen om en stund – inloggade användare får fler frågor.";
        case 502: return "AI-tjänsten är inte tillgänglig just nu.";
        case 503: return "AI-tjänsten är upptagen. Försök igen om en stund.";
        default: return "Något gick fel. Försök igen.";
    }
}

// Egna felmeddelanden för fel som kommer inne i strömmen (efter att 200 redan skickats)
function getStreamErrorMessage(code) {
    switch (code) {
        case "busy":           return "AI-tjänsten är upptagen. Försök igen om en stund.";
        case "unavailable":    return "AI-tjänsten är inte tillgänglig just nu.";
        default:               return "Något gick fel. Försök igen.";
    }
}

// Registrerar konto och loggar in direkt
export async function register(email, password) {
    const response = await postAccount("register", { email, password });

    // För många försök från samma nätverk (rate limiting per IP-adress)
    if (response.status === 429) {
        throw new Error("För många försök. Vänta en stund och försök igen.");
    }

    if (!response.ok) {
        throw new Error(await getRegisterErrorMessage(response));
    }

    // CSRF-token är kopplad till användaren, ny token behövs efter inloggning
    await refreshCsrfToken();
}

// Loggar in
export async function login(email, password) {
    const response = await postAccount("login", { email, password });

    // För många försök från samma nätverk (rate limiting per IP-adress)
    if (response.status === 429) {
        throw new Error("För många försök. Vänta en stund och försök igen.");
    }

    if (!response.ok) {
        // Samma meddelande oavsett orsak (avslöjar inte vilka konton som finns)
        throw new Error("Inloggningen misslyckades. Kontrollera e-post och lösenord. Efter fem misslyckade försök spärras inloggningen i 15 minuter.")
    }

    await refreshCsrfToken();
}

// Loggar ut
export async function logout() {
    await postAccount("logout");
    await refreshCsrfToken();
}

// Returnerar inloggad användares e-post, eller null om ingen är inloggad
export async function getCurrentUser() {
    try {
        const response = await fetch("/api/account/me");
        if (!response.ok) {
            return null;
        }
        const data = await response.json();
        return data.email;
    } catch (error) {
        console.error(error);
        return null;
    }
}

// Skickar POST till ett konto-endpoint med CSRF-token (Cross-Site Request Forgery)
async function postAccount(path, body = null) {
    const token = await getCsrfToken();

    try {
        return await fetch(`/api/account/${path}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
                "X-CSRF-TOKEN": token
            },
            body: body ? JSON.stringify(body) : undefined
        });
    } catch (error) {
        console.error(error);
        throw new Error("Kunde inte nå servern. Kontrollera din anslutning.");
    }
}

// Egna felmeddelanden vid registrering, utifrån felkoder från Identity
async function getRegisterErrorMessage(response) {
    if (response.status === 400) {
        const problem = await response.json().catch(() => null);
        const errorCodes = Object.keys(problem?.errors ?? {});

        if (errorCodes.includes("PasswordTooShort") || errorCodes.includes("password")) {
            return "Lösenordet måste vara minst 12 tecken (och högst 128).";
        }
        if (errorCodes.includes("InvalidEmail") || errorCodes.includes("email")) {
            return "Ange en giltig e-postadress.";
        }
    }
    // Upptagen e-post och övriga fel ger samma meddelande
    return "Kontot kunde inte skapas. Kontrollera uppgifterna och försök igen.";
}

// Hämtar inloggad användares sparade konversation, null om ingen finns eller om användaren inte är inloggad
export async function loadHistory() {
    try {
        const response = await fetch("/api/history");

        // 204 = ingen sparad konversation, 401 = ej inloggad
        if (response.status !== 200) {
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error(error);
        return null;
    }
}

// Raderar inloggad användares sparade konversation
export async function deleteHistory() {
    const token = await getCsrfToken();
    let response;

    try {
        response = await fetch("/api/history", {
            method: "DELETE",
            headers: { "X-CSRF-TOKEN": token }
        });
    } catch (error) {
        console.error(error);
        throw new Error("Kunde inte nå servern. Kontrollera din anslutning.");
    }

    if (!response.ok) {
        throw new Error("Den sparade chatten kunde inte raderas. Försök igen.");
    }
}