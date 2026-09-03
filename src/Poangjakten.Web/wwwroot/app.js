import { api } from "./api-client.js";

const participantKey = "poangjakten.participantId";
const views = {
  loading: document.querySelector("#loading-view"),
  registration: document.querySelector("#registration-view"),
  dashboard: document.querySelector("#dashboard-view"),
  admin: document.querySelector("#admin-view")
};
const registrationForm = document.querySelector("#registration-form");
const displayNameInput = document.querySelector("#display-name");
const registrationError = document.querySelector("#registration-error");
const adminLoginForm = document.querySelector("#admin-login-form");
const adminSecretInput = document.querySelector("#admin-secret");
const adminLoginError = document.querySelector("#admin-login-error");
const welcomeHeading = document.querySelector("#welcome-heading");
const score = document.querySelector("#score");
const scoreBadge = document.querySelector("#score-badge");
const storageMessage = document.querySelector("#storage-message");
const tableStatus = document.querySelector("#table-status");
const blobStatus = document.querySelector("#blob-status");
const testStorageButton = document.querySelector("#test-storage");
const changeParticipantButton = document.querySelector("#change-participant");
const participantList = document.querySelector("#participant-list");
const adminError = document.querySelector("#admin-error");
const featureMessage = document.querySelector("#feature-message");
let toastTimer;

function showView(name) {
  Object.entries(views).forEach(([viewName, element]) => { element.hidden = viewName !== name; });
  window.scrollTo({ top: 0, behavior: "instant" });
}

function showParticipant(participant) {
  welcomeHeading.textContent = `Hej, ${participant.displayName}!`;
  score.textContent = participant.score;
  scoreBadge.hidden = false;
  document.querySelectorAll(".admin-only").forEach(element => { element.hidden = true; });
  showView("dashboard");
}

function showAdmin(session) {
  welcomeHeading.textContent = `Hej, ${session.displayName}!`;
  scoreBadge.hidden = true;
  document.querySelectorAll(".admin-only").forEach(element => { element.hidden = false; });
  showView("dashboard");
}

async function restoreParticipant() {
  try {
    const adminSession = await api.getAdminSession();
    if (adminSession) {
      showAdmin(adminSession);
      return;
    }
  } catch {
    // A stale admin session must not prevent ordinary participant login.
  }

  const participantId = localStorage.getItem(participantKey);
  if (!participantId) {
    showView("registration");
    displayNameInput.focus();
    return;
  }

  try {
    showParticipant(await api.getParticipant(participantId));
  } catch {
    localStorage.removeItem(participantKey);
    showView("registration");
    displayNameInput.focus();
  }
}

registrationForm.addEventListener("submit", async event => {
  event.preventDefault();
  registrationError.hidden = true;
  const submitButton = registrationForm.querySelector("button[type=submit]");
  submitButton.disabled = true;
  try {
    const participant = await api.registerParticipant(displayNameInput.value);
    localStorage.setItem(participantKey, participant.id);
    showParticipant(participant);
  } catch (error) {
    registrationError.textContent = error.message;
    registrationError.hidden = false;
  } finally {
    submitButton.disabled = false;
  }
});

document.querySelector("#show-admin-login").addEventListener("click", () => {
  registrationForm.hidden = true;
  document.querySelector("#show-admin-login").hidden = true;
  adminLoginForm.hidden = false;
  adminSecretInput.focus();
});

document.querySelector("#cancel-admin-login").addEventListener("click", () => {
  adminLoginForm.reset();
  adminLoginError.hidden = true;
  adminLoginForm.hidden = true;
  registrationForm.hidden = false;
  document.querySelector("#show-admin-login").hidden = false;
  displayNameInput.focus();
});

adminLoginForm.addEventListener("submit", async event => {
  event.preventDefault();
  adminLoginError.hidden = true;
  const submitButton = adminLoginForm.querySelector("button[type=submit]");
  submitButton.disabled = true;
  try {
    const session = await api.signInAdmin(adminSecretInput.value);
    if (!session) {
      adminLoginError.textContent = "Fel adminhemlighet.";
      adminLoginError.hidden = false;
      return;
    }
    localStorage.removeItem(participantKey);
    adminLoginForm.reset();
    showAdmin(session);
  } catch (error) {
    adminLoginError.textContent = error.message;
    adminLoginError.hidden = false;
  } finally {
    submitButton.disabled = false;
  }
});

changeParticipantButton.addEventListener("click", async () => {
  try { await api.signOutAdmin(); } catch { /* The local view can still be reset. */ }
  localStorage.removeItem(participantKey);
  registrationForm.reset();
  registrationForm.hidden = false;
  adminLoginForm.hidden = true;
  document.querySelector("#show-admin-login").hidden = false;
  showView("registration");
  displayNameInput.focus();
});

document.querySelectorAll("[data-feature]").forEach(tile => {
  tile.addEventListener("click", () => {
    clearTimeout(toastTimer);
    featureMessage.textContent = "Den här delen öppnar vi i nästa steg.";
    featureMessage.hidden = false;
    toastTimer = setTimeout(() => { featureMessage.hidden = true; }, 2600);
  });
});

document.querySelector("#open-admin").addEventListener("click", async () => {
  showView("admin");
  await loadParticipants();
});

document.querySelector("#close-admin").addEventListener("click", () => showView("dashboard"));

async function loadParticipants() {
  participantList.innerHTML = '<p class="muted">Hämtar deltagare…</p>';
  adminError.hidden = true;
  try {
    renderParticipants(await api.listParticipants());
  } catch (error) {
    participantList.innerHTML = "";
    adminError.textContent = error.message;
    adminError.hidden = false;
  }
}

function renderParticipants(participants) {
  participantList.replaceChildren();
  if (participants.length === 0) {
    participantList.innerHTML = '<p class="muted">Det finns inga deltagare ännu.</p>';
    return;
  }

  participants.forEach(participant => {
    const row = document.createElement("article");
    row.className = "participant-row";

    const info = document.createElement("div");
    const name = document.createElement("p");
    name.className = "participant-name";
    name.textContent = participant.displayName;
    const participantScore = document.createElement("p");
    participantScore.className = "participant-score";
    participantScore.textContent = `${participant.score} poäng`;
    info.append(name, participantScore);

    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "danger-button";
    remove.textContent = "Ta bort";
    remove.addEventListener("click", () => removeParticipant(participant, remove));
    row.append(info, remove);
    participantList.append(row);
  });
}

async function removeParticipant(participant, button) {
  if (!window.confirm(`Ta bort ${participant.displayName}?`)) return;
  button.disabled = true;
  adminError.hidden = true;
  try {
    await api.deleteParticipant(participant.id);
    if (localStorage.getItem(participantKey) === participant.id) {
      localStorage.removeItem(participantKey);
    }
    await loadParticipants();
  } catch (error) {
    adminError.textContent = error.message;
    adminError.hidden = false;
    button.disabled = false;
  }
}

testStorageButton.addEventListener("click", async () => {
  testStorageButton.disabled = true;
  storageMessage.textContent = "Testar skrivning, läsning och radering…";
  tableStatus.textContent = "◌ Table Storage";
  blobStatus.textContent = "◌ Blob Storage";
  try {
    const result = await api.testStorage();
    tableStatus.textContent = `${result.tableStorage ? "✓" : "✕"} Table Storage`;
    blobStatus.textContent = `${result.blobStorage ? "✓" : "✕"} Blob Storage`;
    storageMessage.textContent = result.isHealthy ? "All lagring fungerar" : result.error;
  } catch (error) {
    storageMessage.textContent = error.message;
    tableStatus.textContent = "? Table Storage";
    blobStatus.textContent = "? Blob Storage";
  } finally {
    testStorageButton.disabled = false;
  }
});

restoreParticipant();
