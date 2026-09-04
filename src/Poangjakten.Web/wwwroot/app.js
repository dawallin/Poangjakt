import { api } from "./api-client.js";

const participantKey = "poangjakten.participantId";
const views = {
  loading: document.querySelector("#loading-view"),
  registration: document.querySelector("#registration-view"),
  dashboard: document.querySelector("#dashboard-view"),
  adminUsers: document.querySelector("#admin-users-view"),
  adminChallenges: document.querySelector("#admin-challenges-view")
};
const registrationForm = document.querySelector("#registration-form");
const displayNameInput = document.querySelector("#display-name");
const registrationError = document.querySelector("#registration-error");
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
const challengeForm = document.querySelector("#challenge-form");
const challengeId = document.querySelector("#challenge-id");
const challengeDescription = document.querySelector("#challenge-description");
const challengePoints = document.querySelector("#challenge-points");
const challengeError = document.querySelector("#challenge-error");
const challengeList = document.querySelector("#challenge-list");
const saveChallengeButton = document.querySelector("#save-challenge");
const cancelChallengeEditButton = document.querySelector("#cancel-challenge-edit");
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
    const adminSession = await api.signInAdmin(displayNameInput.value);
    if (adminSession) {
      localStorage.removeItem(participantKey);
      registrationForm.reset();
      showAdmin(adminSession);
      return;
    }

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

changeParticipantButton.addEventListener("click", async () => {
  try { await api.signOutAdmin(); } catch { /* The local view can still be reset. */ }
  localStorage.removeItem(participantKey);
  registrationForm.reset();
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

document.querySelector("#open-admin-users").addEventListener("click", async () => {
  showView("adminUsers");
  await loadParticipants();
});

document.querySelector("#open-admin-challenges").addEventListener("click", async () => {
  showView("adminChallenges");
  resetChallengeForm();
  await loadChallenges();
});

document.querySelectorAll(".close-admin").forEach(button => {
  button.addEventListener("click", () => showView("dashboard"));
});

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

async function loadChallenges() {
  challengeList.innerHTML = '<p class="muted">Hämtar uppgifter…</p>';
  challengeError.hidden = true;
  try {
    renderChallenges(await api.listChallenges());
  } catch (error) {
    challengeList.innerHTML = "";
    challengeError.textContent = error.message;
    challengeError.hidden = false;
  }
}

function renderChallenges(challenges) {
  challengeList.replaceChildren();
  if (challenges.length === 0) {
    challengeList.innerHTML = '<p class="muted">Det finns inga poänguppgifter ännu.</p>';
    return;
  }

  const groups = challenges.reduce((result, challenge) => {
    const items = result.get(challenge.points) ?? [];
    items.push(challenge);
    result.set(challenge.points, items);
    return result;
  }, new Map());
  [...groups.entries()].sort(([left], [right]) => left - right).forEach(([points, items]) => {
    const section = document.createElement("section");
    const heading = document.createElement("h2");
    heading.className = "challenge-group-title";
    heading.textContent = `${points} poäng`;
    const rows = document.createElement("div");
    rows.className = "challenge-items";

    items.forEach(challenge => {
      const row = document.createElement("article");
      row.className = "challenge-row";
      const description = document.createElement("p");
      description.className = "challenge-description";
      description.textContent = challenge.description;

      const actions = document.createElement("div");
      actions.className = "icon-actions";
      const edit = document.createElement("button");
      edit.type = "button";
      edit.className = "icon-button";
      edit.textContent = "✎";
      edit.setAttribute("aria-label", `Ändra ${challenge.description}`);
      edit.addEventListener("click", () => beginChallengeEdit(challenge));
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "icon-button danger";
      remove.textContent = "🗑";
      remove.setAttribute("aria-label", `Ta bort ${challenge.description}`);
      remove.addEventListener("click", () => removeChallenge(challenge, remove));
      actions.append(edit, remove);
      row.append(description, actions);
      rows.append(row);
    });

    section.append(heading, rows);
    challengeList.append(section);
  });
}

challengeForm.addEventListener("submit", async event => {
  event.preventDefault();
  challengeError.hidden = true;
  saveChallengeButton.disabled = true;
  try {
    const points = Number.parseInt(challengePoints.value, 10);
    if (challengeId.value) {
      await api.updateChallenge(challengeId.value, challengeDescription.value, points);
    } else {
      await api.createChallenge(challengeDescription.value, points);
    }
    resetChallengeForm();
    await loadChallenges();
  } catch (error) {
    challengeError.textContent = error.message;
    challengeError.hidden = false;
  } finally {
    saveChallengeButton.disabled = false;
  }
});

cancelChallengeEditButton.addEventListener("click", resetChallengeForm);

function beginChallengeEdit(challenge) {
  challengeId.value = challenge.id;
  challengeDescription.value = challenge.description;
  challengePoints.value = challenge.points;
  saveChallengeButton.textContent = "Spara ändringar";
  cancelChallengeEditButton.hidden = false;
  challengeDescription.focus();
  challengeForm.scrollIntoView({ behavior: "smooth", block: "start" });
}

function resetChallengeForm() {
  challengeForm.reset();
  challengeId.value = "";
  saveChallengeButton.textContent = "Lägg till uppgift";
  cancelChallengeEditButton.hidden = true;
  challengeError.hidden = true;
}

async function removeChallenge(challenge, button) {
  if (!window.confirm(`Ta bort uppgiften ”${challenge.description}”?`)) return;
  button.disabled = true;
  challengeError.hidden = true;
  try {
    await api.deleteChallenge(challenge.id);
    if (challengeId.value === challenge.id) resetChallengeForm();
    await loadChallenges();
  } catch (error) {
    challengeError.textContent = error.message;
    challengeError.hidden = false;
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
