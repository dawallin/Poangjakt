import { api } from "./api-client.js?v=20260905-8";
import { compressImage, compressPhoto } from "./image-utils.js";

const participantKey = "poangjakten.participantId";
const views = {
  loading: document.querySelector("#loading-view"),
  registration: document.querySelector("#registration-view"),
  dashboard: document.querySelector("#dashboard-view"),
  table: document.querySelector("#table-view"),
  leaderboard: document.querySelector("#leaderboard-view"),
  tableLeaderboard: document.querySelector("#table-leaderboard-view"),
  challenges: document.querySelector("#challenges-view"),
  tableChallenges: document.querySelector("#table-challenges-view"),
  photos: document.querySelector("#photos-view"),
  songs: document.querySelector("#songs-view"),
  songRequests: document.querySelector("#song-requests-view"),
  adminUsers: document.querySelector("#admin-users-view"),
  adminStages: document.querySelector("#admin-stages-view"),
  adminChallenges: document.querySelector("#admin-challenges-view"),
  adminSongs: document.querySelector("#admin-songs-view")
};
const registrationForm = document.querySelector("#registration-form");
const loginCodeInput = document.querySelector("#login-code");
const registrationError = document.querySelector("#registration-error");
const welcomeHeading = document.querySelector("#welcome-heading");
const clueCard = document.querySelector("#clue-card");
const participantClue = document.querySelector("#participant-clue");
const tableTile = document.querySelector("#open-table");
const tableTileSymbol = document.querySelector("#table-tile-symbol");
const tableTileCaption = document.querySelector("#table-tile-caption");
const tableChallengeTile = document.querySelector("#open-table-challenges");
const tableChallengeTileSymbol = document.querySelector("#table-challenge-tile-symbol");
const tableChallengeTileCaption = document.querySelector("#table-challenge-tile-caption");
const tableLeaderboardTile = document.querySelector("#open-table-leaderboard");
const tableLeaderboardTileSymbol = document.querySelector("#table-leaderboard-tile-symbol");
const tableLeaderboardTileCaption = document.querySelector("#table-leaderboard-tile-caption");
const songRequestTile = document.querySelector("#open-song-requests");
const songRequestTileSymbol = document.querySelector("#song-request-tile-symbol");
const songRequestTileCaption = document.querySelector("#song-request-tile-caption");
const participantTableResult = document.querySelector("#participant-table-result");
const participantTableError = document.querySelector("#participant-table-error");
const score = document.querySelector("#score");
const scoreBadge = document.querySelector("#score-badge");
const challengeScore = document.querySelector("#challenge-score");
const tableChallengeScore = document.querySelector("#table-challenge-score");
const playerChallengeList = document.querySelector("#player-challenge-list");
const playerChallengeError = document.querySelector("#player-challenge-error");
const specialQuestionList = document.querySelector("#special-question-list");
const tableChallengeList = document.querySelector("#table-challenge-list");
const tableChallengeError = document.querySelector("#table-challenge-error");
const storageMessage = document.querySelector("#storage-message");
const tableStatus = document.querySelector("#table-status");
const blobStatus = document.querySelector("#blob-status");
const testStorageButton = document.querySelector("#test-storage");
const changeParticipantButton = document.querySelector("#change-participant");
const participantList = document.querySelector("#participant-list");
const adminError = document.querySelector("#admin-error");
const participantForm = document.querySelector("#participant-form");
const participantId = document.querySelector("#participant-id");
const participantName = document.querySelector("#participant-name");
const participantCode = document.querySelector("#participant-code");
const participantClueInput = document.querySelector("#participant-clue-input");
const participantTable = document.querySelector("#participant-table");
const saveParticipantButton = document.querySelector("#save-participant");
const cancelParticipantEditButton = document.querySelector("#cancel-participant-edit");
const adminStageList = document.querySelector("#admin-stage-list");
const adminStageError = document.querySelector("#admin-stage-error");
const challengeForm = document.querySelector("#challenge-form");
const challengeId = document.querySelector("#challenge-id");
const challengeDescription = document.querySelector("#challenge-description");
const challengePoints = document.querySelector("#challenge-points");
const challengeScope = document.querySelector("#challenge-scope");
const challengeUnlockStage = document.querySelector("#challenge-unlock-stage");
const challengeError = document.querySelector("#challenge-error");
const challengeList = document.querySelector("#challenge-list");
const saveChallengeButton = document.querySelector("#save-challenge");
const cancelChallengeEditButton = document.querySelector("#cancel-challenge-edit");
const featureMessage = document.querySelector("#feature-message");
const photoUploadForm = document.querySelector("#photo-upload-form");
const photoFiles = document.querySelector("#photo-files");
const photoSelection = document.querySelector("#photo-selection");
const uploadPhotosButton = document.querySelector("#upload-photos");
const photoUploadStatus = document.querySelector("#photo-upload-status");
const photoGallery = document.querySelector("#photo-gallery");
const photoError = document.querySelector("#photo-error");
const photoDialog = document.querySelector("#photo-dialog");
const dialogPhoto = document.querySelector("#dialog-photo");
const photoDialogMeta = document.querySelector("#photo-dialog-meta");
const leaderboardList = document.querySelector("#leaderboard-list");
const leaderboardError = document.querySelector("#leaderboard-error");
const tableLeaderboardList = document.querySelector("#table-leaderboard-list");
const tableLeaderboardError = document.querySelector("#table-leaderboard-error");
const songList = document.querySelector("#song-list");
const songError = document.querySelector("#song-error");
const songForm = document.querySelector("#song-form");
const songId = document.querySelector("#song-id");
const songTitle = document.querySelector("#song-title");
const songMelody = document.querySelector("#song-melody");
const songLyrics = document.querySelector("#song-lyrics");
const songSortOrder = document.querySelector("#song-sort-order");
const songImage = document.querySelector("#song-image");
const songCurrentImage = document.querySelector("#song-current-image");
const songCurrentImagePreview = document.querySelector("#song-current-image-preview");
const removeSongImageButton = document.querySelector("#remove-song-image");
const adminSongError = document.querySelector("#admin-song-error");
const adminSongStatus = document.querySelector("#admin-song-status");
const adminSongList = document.querySelector("#admin-song-list");
const saveSongButton = document.querySelector("#save-song");
const cancelSongEditButton = document.querySelector("#cancel-song-edit");
const songRequestForm = document.querySelector("#song-request-form");
const songRequestArtist = document.querySelector("#song-request-artist");
const songRequestTitle = document.querySelector("#song-request-title");
const saveSongRequestButton = document.querySelector("#save-song-request");
const songRequestStatus = document.querySelector("#song-request-status");
const songRequestError = document.querySelector("#song-request-error");
const songRequestList = document.querySelector("#song-request-list");
let toastTimer;
let currentParticipantId = null;
let currentIsAdmin = false;
let currentParticipantHasTable = false;
let currentSong = null;
let partyTables = [];

function showView(name) {
  Object.entries(views).forEach(([viewName, element]) => { element.hidden = viewName !== name; });
  window.scrollTo({ top: 0, behavior: "instant" });
}

function showParticipant(participant) {
  currentParticipantId = participant.id;
  currentIsAdmin = false;
  currentParticipantHasTable = participant.hasTable;
  welcomeHeading.textContent = `Hej, ${participant.displayName}!`;
  participantClue.textContent = participant.clue;
  score.textContent = participant.score;
  scoreBadge.hidden = false;
  document.querySelectorAll(".admin-only").forEach(element => { element.hidden = true; });
  document.querySelectorAll(".participant-only").forEach(element => { element.hidden = false; });
  clueCard.hidden = !participant.clue;
  document.querySelectorAll(".participant-table-only").forEach(element => {
    element.hidden = !participant.hasTable;
  });
  showView("dashboard");
  refreshPartyStageSummary();
}

function showAdmin(session) {
  currentParticipantId = null;
  currentIsAdmin = true;
  currentParticipantHasTable = false;
  welcomeHeading.textContent = `Hej, ${session.displayName}!`;
  scoreBadge.hidden = true;
  document.querySelectorAll(".admin-only").forEach(element => { element.hidden = false; });
  document.querySelectorAll(".participant-only").forEach(element => { element.hidden = true; });
  document.querySelectorAll(".participant-table-only").forEach(element => { element.hidden = false; });
  showView("dashboard");
  refreshPartyStageSummary();
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
    loginCodeInput.focus();
    return;
  }

  try {
    showParticipant(await api.getParticipant(participantId));
  } catch {
    localStorage.removeItem(participantKey);
    showView("registration");
    loginCodeInput.focus();
  }
}

registrationForm.addEventListener("submit", async event => {
  event.preventDefault();
  registrationError.hidden = true;
  const submitButton = registrationForm.querySelector("button[type=submit]");
  submitButton.disabled = true;
  try {
    const code = loginCodeInput.value;
    const adminSession = await api.signInAdmin(code);
    if (adminSession) {
      localStorage.removeItem(participantKey);
      registrationForm.reset();
      showAdmin(adminSession);
      return;
    }

    const participant = await api.loginParticipant(code);
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
  loginCodeInput.focus();
});

async function refreshPartyStageSummary() {
  try {
    const stages = await api.listPartyStages();
    const tableStage = stages.find(stage => stage.id === "table-reveal");
    setTableTileState(tableStage?.isUnlocked === true);
  } catch {
    setTableTileState(false);
  }
}

function setTableTileState(isUnlocked) {
  tableTile.classList.toggle("locked", !isUnlocked);
  tableTileSymbol.textContent = isUnlocked ? "●" : "🔒";
  tableTileCaption.textContent = isUnlocked ? "Se bordet och dina bordskamrater" : "Väntar på att låsas upp";
  tableChallengeTile.classList.toggle("locked", !isUnlocked);
  tableChallengeTileSymbol.textContent = isUnlocked ? "✓" : "🔒";
  tableChallengeTileCaption.textContent = isUnlocked
    ? "Kryssa i det bordet har gjort"
    : "Låses upp tillsammans med borden";
  tableLeaderboardTile.classList.toggle("locked", !isUnlocked);
  tableLeaderboardTileSymbol.textContent = isUnlocked ? "▲" : "🔒";
  tableLeaderboardTileCaption.textContent = isUnlocked
    ? "Se bordens ställning"
    : "Låses upp tillsammans med borden";
  songRequestTile.classList.toggle("locked", !isUnlocked);
  songRequestTileSymbol.textContent = isUnlocked ? "♫" : "🔒";
  songRequestTileCaption.textContent = isUnlocked
    ? "Önska musik tillsammans med bordet"
    : "Låses upp tillsammans med borden";
}

tableTile.addEventListener("click", async () => {
  if (!currentParticipantId || !currentParticipantHasTable) {
    showToast("Admin har ingen egen bordsplacering.");
    return;
  }

  showView("table");
  await loadParticipantTable();
});

document.querySelector("#close-table").addEventListener("click", () => showView("dashboard"));

async function loadParticipantTable() {
  participantTableResult.innerHTML = '<p class="muted">Hämtar ditt bord…</p>';
  participantTableError.hidden = true;
  try {
    const table = await api.getParticipantTable(currentParticipantId);
    setTableTileState(true);
    renderParticipantTable(table);
  } catch (error) {
    participantTableResult.replaceChildren();
    participantTableError.textContent = error.message;
    participantTableError.hidden = false;
    setTableTileState(false);
  }
}

function renderParticipantTable(table) {
  participantTableResult.replaceChildren();
  const card = document.createElement("section");
  card.className = "table-card";
  const label = document.createElement("p");
  label.className = "table-number";
  label.textContent = `Bord ${table.number}`;
  const heading = document.createElement("h2");
  heading.textContent = table.name;
  const membersHeading = document.createElement("h3");
  membersHeading.textContent = "Ni som sitter här";
  const members = document.createElement("ul");
  members.className = "table-member-list";

  table.members.forEach(member => {
    const item = document.createElement("li");
    if (member.isCurrentParticipant) item.className = "current-table-member";
    item.textContent = member.isCurrentParticipant
      ? `${member.displayName} (du)`
      : member.displayName;
    members.append(item);
  });

  card.append(label, heading, membersHeading, members);
  participantTableResult.append(card);
}

document.querySelector("#open-challenges").addEventListener("click", async () => {
  if (!currentParticipantId) {
    showToast("Admin har ingen egen deltagarpoäng.");
    return;
  }
  showView("challenges");
  challengeScore.textContent = score.textContent;
  await loadPlayerChallenges();
});

document.querySelector("#close-challenges").addEventListener("click", () => showView("dashboard"));

tableChallengeTile.addEventListener("click", async () => {
  if (!currentParticipantId || !currentParticipantHasTable) {
    showToast("Admin har inget eget bordslag.");
    return;
  }
  showView("tableChallenges");
  await loadTableChallenges();
});

document.querySelector("#close-table-challenges").addEventListener("click", () => showView("dashboard"));

document.querySelector("#open-leaderboard").addEventListener("click", async () => {
  showView("leaderboard");
  await loadLeaderboard();
});

document.querySelector("#close-leaderboard").addEventListener("click", () => showView("dashboard"));
document.querySelector("#refresh-leaderboard").addEventListener("click", loadLeaderboard);

tableLeaderboardTile.addEventListener("click", async () => {
  if (!currentParticipantId || !currentParticipantHasTable) {
    showToast("Admin har inget eget bordslag.");
    return;
  }
  showView("tableLeaderboard");
  await loadTableLeaderboard();
});

document.querySelector("#close-table-leaderboard").addEventListener("click", () => showView("dashboard"));
document.querySelector("#refresh-table-leaderboard").addEventListener("click", loadTableLeaderboard);

async function loadLeaderboard() {
  leaderboardList.innerHTML = '<p class="muted">Hämtar ställningen…</p>';
  leaderboardError.hidden = true;
  try {
    renderLeaderboard(await api.listLeaderboard());
  } catch (error) {
    leaderboardList.replaceChildren();
    leaderboardError.textContent = error.message;
    leaderboardError.hidden = false;
  }
}

function renderLeaderboard(participants) {
  leaderboardList.replaceChildren();
  if (participants.length === 0) {
    leaderboardList.innerHTML = '<p class="muted">Inga deltagare har gått med ännu.</p>';
    return;
  }

  let previousScore = null;
  let rank = 0;
  participants.forEach((participant, index) => {
    if (participant.score !== previousScore) rank = index + 1;
    previousScore = participant.score;

    const entry = document.createElement("div");
    entry.className = "leaderboard-entry";
    const row = document.createElement("button");
    row.type = "button";
    row.className = `leaderboard-row rank-${Math.min(rank, 4)}`;
    if (participant.id === currentParticipantId) row.classList.add("current-participant");
    row.setAttribute("aria-expanded", "false");

    const position = document.createElement("span");
    position.className = "leaderboard-rank";
    position.textContent = `${rank}`;
    const name = document.createElement("span");
    name.className = "leaderboard-name";
    const nameValue = document.createElement("strong");
    nameValue.textContent = participant.displayName;
    const hint = document.createElement("small");
    hint.textContent = "Visa uppgifter";
    name.append(nameValue, hint);
    const points = document.createElement("span");
    points.className = "leaderboard-score";
    const pointValue = document.createElement("strong");
    pointValue.textContent = participant.score;
    const pointLabel = document.createElement("small");
    pointLabel.textContent = "poäng";
    points.append(pointValue, pointLabel);
    row.append(position, name, points);

    const details = document.createElement("section");
    details.className = "leaderboard-details";
    details.hidden = true;
    row.addEventListener("click", async () => {
      if (!details.hidden) {
        details.hidden = true;
        row.setAttribute("aria-expanded", "false");
        hint.textContent = "Visa uppgifter";
        return;
      }

      details.hidden = false;
      row.setAttribute("aria-expanded", "true");
      hint.textContent = "Dölj uppgifter";
      if (details.dataset.loaded === "true") return;
      details.innerHTML = '<p class="muted">Hämtar uppgifter…</p>';
      try {
        renderChallengeSummary(
          await api.getParticipantChallengeSummary(participant.id),
          details,
          false);
        details.dataset.loaded = "true";
      } catch (error) {
        renderLeaderboardDetailError(details, error.message);
      }
    });

    entry.append(row, details);
    leaderboardList.append(entry);
  });
}

async function loadTableLeaderboard() {
  tableLeaderboardList.innerHTML = '<p class="muted">Hämtar ställningen…</p>';
  tableLeaderboardError.hidden = true;
  try {
    const tables = await api.listTableLeaderboard(currentParticipantId);
    setTableTileState(true);
    renderTableLeaderboard(tables);
  } catch (error) {
    tableLeaderboardList.replaceChildren();
    tableLeaderboardError.textContent = error.message;
    tableLeaderboardError.hidden = false;
    setTableTileState(false);
  }
}

function renderTableLeaderboard(tables) {
  tableLeaderboardList.replaceChildren();
  let previousScore = null;
  let rank = 0;
  tables.forEach((table, index) => {
    if (table.score !== previousScore) rank = index + 1;
    previousScore = table.score;

    const entry = document.createElement("div");
    entry.className = "leaderboard-entry";
    const row = document.createElement("button");
    row.type = "button";
    row.className = `leaderboard-row rank-${Math.min(rank, 4)}`;
    if (table.isCurrentTable) row.classList.add("current-participant");
    row.setAttribute("aria-expanded", "false");
    const position = document.createElement("span");
    position.className = "leaderboard-rank";
    position.textContent = `${rank}`;
    const name = document.createElement("span");
    name.className = "leaderboard-name";
    const nameValue = document.createElement("strong");
    nameValue.textContent = table.displayName;
    const hint = document.createElement("small");
    hint.textContent = "Visa uppgifter";
    name.append(nameValue, hint);
    const points = document.createElement("span");
    points.className = "leaderboard-score";
    const pointValue = document.createElement("strong");
    pointValue.textContent = table.score;
    const pointLabel = document.createElement("small");
    pointLabel.textContent = "poäng";
    points.append(pointValue, pointLabel);
    row.append(position, name, points);

    const details = document.createElement("section");
    details.className = "leaderboard-details";
    details.hidden = true;
    row.addEventListener("click", async () => {
      if (!details.hidden) {
        details.hidden = true;
        row.setAttribute("aria-expanded", "false");
        hint.textContent = "Visa uppgifter";
        return;
      }

      details.hidden = false;
      row.setAttribute("aria-expanded", "true");
      hint.textContent = "Dölj uppgifter";
      if (details.dataset.loaded === "true") return;
      details.innerHTML = '<p class="muted">Hämtar uppgifter…</p>';
      try {
        renderChallengeSummary(
          await api.getTableChallengeSummary(currentParticipantId, table.id),
          details,
          true);
        details.dataset.loaded = "true";
      } catch (error) {
        renderLeaderboardDetailError(details, error.message);
      }
    });

    entry.append(row, details);
    tableLeaderboardList.append(entry);
  });
}

function renderChallengeSummary(summary, target, isTable) {
  target.replaceChildren();
  const total = document.createElement("p");
  total.className = "leaderboard-detail-total";
  total.textContent = `${summary.score} poäng totalt`;
  target.append(total);

  const items = [
    ...summary.challenges.map(challenge => ({
      text: challenge.description,
      points: challenge.points
    })),
    ...(summary.specialQuestions ?? []).map(question => ({
      text: `${question.prompt} ${question.value} %`,
      points: question.points,
      special: true
    }))
  ];

  if (items.length === 0) {
    const empty = document.createElement("p");
    empty.className = "muted";
    empty.textContent = isTable
      ? "Bordet har inte klarat någon synlig uppgift ännu."
      : "Deltagaren har inte klarat någon synlig uppgift ännu.";
    target.append(empty);
    return;
  }

  const list = document.createElement("ul");
  list.className = "leaderboard-challenge-list";
  items.forEach(item => {
    const listItem = document.createElement("li");
    const description = document.createElement("span");
    description.textContent = item.text;
    if (item.special) description.className = "special-summary-item";
    const points = document.createElement("strong");
    points.textContent = `${item.points} p`;
    listItem.append(description, points);
    list.append(listItem);
  });
  target.append(list);
}

function renderLeaderboardDetailError(target, message) {
  target.replaceChildren();
  const error = document.createElement("p");
  error.className = "form-error";
  error.textContent = message;
  target.append(error);
}

document.querySelector("#open-photos").addEventListener("click", async () => {
  showView("photos");
  await loadPhotos();
});

document.querySelector("#close-photos").addEventListener("click", () => showView("dashboard"));
document.querySelector("#refresh-photos").addEventListener("click", loadPhotos);

document.querySelector("#open-songs").addEventListener("click", async () => {
  showView("songs");
  await loadSongs();
});

document.querySelector("#close-songs").addEventListener("click", () => showView("dashboard"));

songRequestTile.addEventListener("click", async () => {
  if (!currentParticipantId && !currentIsAdmin) return;
  showView("songRequests");
  songRequestStatus.hidden = true;
  await loadSongRequests();
});

document.querySelector("#close-song-requests").addEventListener("click", () => showView("dashboard"));
document.querySelector("#refresh-song-requests").addEventListener("click", loadSongRequests);

songRequestForm.addEventListener("submit", async event => {
  event.preventDefault();
  songRequestError.hidden = true;
  songRequestStatus.hidden = true;
  if (!currentParticipantId) return;

  saveSongRequestButton.disabled = true;
  try {
    await api.createSongRequest(currentParticipantId, songRequestArtist.value, songRequestTitle.value);
    songRequestForm.reset();
    songRequestStatus.textContent = "Låten är tillagd på ert bord!";
    songRequestStatus.hidden = false;
    await loadSongRequests();
    songRequestArtist.focus();
  } catch (error) {
    songRequestError.textContent = error.message;
    songRequestError.hidden = false;
    if (error.message.includes("låses upp")) setTableTileState(false);
  } finally {
    saveSongRequestButton.disabled = false;
  }
});

async function loadSongRequests() {
  songRequestList.innerHTML = '<p class="muted">Hämtar låtönskemål…</p>';
  songRequestError.hidden = true;
  try {
    const songRequests = currentIsAdmin
      ? await api.listAdminSongRequests()
      : await api.listSongRequests(currentParticipantId);
    if (!currentIsAdmin) setTableTileState(true);
    renderSongRequests(songRequests);
  } catch (error) {
    songRequestList.replaceChildren();
    songRequestError.textContent = error.message;
    songRequestError.hidden = false;
    if (!currentIsAdmin) setTableTileState(false);
  }
}

function renderSongRequests(songRequests) {
  songRequestList.replaceChildren();
  if (songRequests.length === 0) {
    songRequestList.innerHTML = '<p class="muted">Inga låtar har önskats ännu. Bli först!</p>';
    return;
  }

  songRequests.forEach(songRequest => {
    const row = document.createElement("article");
    row.className = "song-request-row";
    if (songRequest.isOwnGroup) row.classList.add("own-table");

    const info = document.createElement("div");
    const title = document.createElement("p");
    title.className = "song-request-title";
    title.textContent = songRequest.title;
    const artist = document.createElement("p");
    artist.className = "song-request-artist";
    artist.textContent = songRequest.artist;
    const table = document.createElement("p");
    table.className = "song-request-table";
    if (songRequest.isTableRequest) {
      table.textContent = songRequest.isOwnGroup
        ? `${songRequest.tableDisplayName} · ert bord`
        : songRequest.tableDisplayName;
    } else {
      table.textContent = songRequest.isOwnGroup
        ? "Ditt önskemål"
        : `Önskad av ${songRequest.tableDisplayName}`;
    }
    info.append(title, artist, table);
    row.append(info);

    const actions = document.createElement("div");
    actions.className = "song-request-actions";
    const spotifyLink = document.createElement("a");
    spotifyLink.className = "spotify-link";
    spotifyLink.href = `https://open.spotify.com/search/${encodeURIComponent(`${songRequest.artist} ${songRequest.title}`)}`;
    spotifyLink.target = "_blank";
    spotifyLink.rel = "noopener noreferrer";
    spotifyLink.textContent = "Spotify ↗";
    spotifyLink.setAttribute("aria-label", `Sök efter ${songRequest.title} av ${songRequest.artist} på Spotify`);
    actions.append(spotifyLink);

    if (currentIsAdmin || songRequest.isOwnGroup) {
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "icon-button danger";
      remove.textContent = "🗑";
      remove.setAttribute("aria-label", `Ta bort ${songRequest.title} av ${songRequest.artist}`);
      remove.addEventListener("click", () => removeSongRequest(songRequest, remove));
      actions.append(remove);
    }

    row.append(actions);
    songRequestList.append(row);
  });
}

async function removeSongRequest(songRequest, button) {
  if (!window.confirm(`Ta bort ”${songRequest.title}” av ${songRequest.artist}?`)) return;
  button.disabled = true;
  songRequestError.hidden = true;
  try {
    if (currentIsAdmin) {
      await api.deleteSongRequest(songRequest.id);
    } else {
      await api.deleteTableSongRequest(currentParticipantId, songRequest.id);
    }
    await loadSongRequests();
  } catch (error) {
    songRequestError.textContent = error.message;
    songRequestError.hidden = false;
    button.disabled = false;
  }
}

photoFiles.addEventListener("change", () => {
  const count = photoFiles.files?.length ?? 0;
  photoSelection.textContent = count === 0
    ? "Du kan välja flera bilder samtidigt."
    : count === 1 ? "1 bild vald" : `${count} bilder valda`;
});

photoUploadForm.addEventListener("submit", async event => {
  event.preventDefault();
  const files = [...(photoFiles.files ?? [])];
  photoError.hidden = true;
  photoUploadStatus.hidden = true;

  if (!currentParticipantId || files.length === 0) {
    photoError.textContent = "Välj minst en bild att ladda upp.";
    photoError.hidden = false;
    return;
  }

  if (files.length > 10) {
    photoError.textContent = "Välj högst 10 bilder åt gången.";
    photoError.hidden = false;
    return;
  }

  uploadPhotosButton.disabled = true;
  photoFiles.disabled = true;
  let uploaded = 0;
  let uploadFailure = null;

  try {
    for (const [index, file] of files.entries()) {
      photoUploadStatus.textContent = `Förbereder bild ${index + 1} av ${files.length}…`;
      photoUploadStatus.hidden = false;
      const compressed = await compressPhoto(file);
      photoUploadStatus.textContent = `Laddar upp bild ${index + 1} av ${files.length}…`;
      await api.uploadPhoto(currentParticipantId, compressed.image, compressed.thumbnail);
      uploaded += 1;
    }

    photoUploadForm.reset();
    photoSelection.textContent = "Du kan välja flera bilder samtidigt.";
    photoUploadStatus.textContent = uploaded === 1
      ? "Bilden är uppladdad!"
      : `${uploaded} bilder är uppladdade!`;
  } catch (error) {
    uploadFailure = uploaded > 0
      ? `${uploaded} bilder laddades upp. Nästa bild misslyckades: ${error.message}`
      : error.message;
    photoUploadStatus.hidden = true;
  } finally {
    uploadPhotosButton.disabled = false;
    photoFiles.disabled = false;
    if (uploaded > 0) await loadPhotos();
    if (uploadFailure) {
      photoError.textContent = uploadFailure;
      photoError.hidden = false;
    }
  }
});

async function loadPhotos() {
  photoGallery.innerHTML = '<p class="muted">Hämtar bilder…</p>';
  photoError.hidden = true;
  try {
    renderPhotos(await api.listPhotos());
  } catch (error) {
    photoGallery.replaceChildren();
    photoError.textContent = error.message;
    photoError.hidden = false;
  }
}

function renderPhotos(photos) {
  photoGallery.replaceChildren();
  if (photos.length === 0) {
    photoGallery.innerHTML = '<p class="muted empty-gallery">Inga bilder ännu. Bli först med att fånga kvällen!</p>';
    return;
  }

  photos.forEach(photo => {
    const card = document.createElement("article");
    card.className = "photo-card";

    const preview = document.createElement("button");
    preview.type = "button";
    preview.className = "photo-preview";
    preview.setAttribute("aria-label", `Öppna bild tagen av ${photo.photographerDisplayName}`);
    preview.addEventListener("click", () => openPhoto(photo));

    const image = document.createElement("img");
    image.src = photo.thumbnailUrl;
    image.alt = `Bild tagen av ${photo.photographerDisplayName}`;
    image.loading = "lazy";
    image.decoding = "async";
    preview.append(image);

    const details = document.createElement("div");
    details.className = "photo-details";
    const photographer = document.createElement("strong");
    photographer.textContent = photo.photographerDisplayName;
    const uploadedAt = document.createElement("time");
    uploadedAt.dateTime = photo.uploadedAt;
    uploadedAt.textContent = formatPhotoTime(photo.uploadedAt);
    details.append(photographer, uploadedAt);

    card.append(preview, details);

    const canDelete = currentIsAdmin || photo.participantId === currentParticipantId;
    if (canDelete) {
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "photo-delete danger-button";
      remove.textContent = "Ta bort";
      remove.setAttribute("aria-label", `Ta bort bild tagen av ${photo.photographerDisplayName}`);
      remove.addEventListener("click", () => removePhoto(photo, remove));
      card.append(remove);
    }

    photoGallery.append(card);
  });
}

function openPhoto(photo) {
  dialogPhoto.src = photo.imageUrl;
  dialogPhoto.alt = `Bild tagen av ${photo.photographerDisplayName}`;
  photoDialogMeta.textContent = `${photo.photographerDisplayName} · ${formatPhotoTime(photo.uploadedAt)}`;
  photoDialog.showModal();
}

document.querySelector("#close-photo-dialog").addEventListener("click", () => photoDialog.close());
photoDialog.addEventListener("click", event => {
  if (event.target === photoDialog) photoDialog.close();
});
photoDialog.addEventListener("close", () => {
  dialogPhoto.removeAttribute("src");
  dialogPhoto.alt = "";
});

async function removePhoto(photo, button) {
  if (!window.confirm(`Ta bort bilden från ${photo.photographerDisplayName}?`)) return;
  button.disabled = true;
  photoError.hidden = true;
  try {
    if (currentIsAdmin) {
      await api.deletePhoto(photo.id);
    } else {
      await api.deleteOwnPhoto(currentParticipantId, photo.id);
    }
    await loadPhotos();
  } catch (error) {
    photoError.textContent = error.message;
    photoError.hidden = false;
    button.disabled = false;
  }
}

function formatPhotoTime(value) {
  return new Intl.DateTimeFormat("sv-SE", {
    day: "numeric",
    month: "short",
    hour: "2-digit",
    minute: "2-digit"
  }).format(new Date(value));
}

async function loadSongs() {
  songList.innerHTML = '<p class="muted">Hämtar sånger…</p>';
  songError.hidden = true;
  try {
    renderSongs(await api.listSongs());
  } catch (error) {
    songList.replaceChildren();
    songError.textContent = error.message;
    songError.hidden = false;
  }
}

function renderSongs(songs) {
  songList.replaceChildren();
  if (songs.length === 0) {
    songList.innerHTML = '<p class="muted">Inga sånger har lagts in ännu.</p>';
    return;
  }

  songs.forEach((song, index) => {
    const article = document.createElement("article");
    article.className = "song-card";

    const toggle = document.createElement("button");
    toggle.type = "button";
    toggle.className = "song-toggle";
    toggle.setAttribute("aria-expanded", "false");

    const number = document.createElement("span");
    number.className = "song-number";
    number.textContent = `${index + 1}`;
    const heading = document.createElement("span");
    heading.className = "song-heading";
    const title = document.createElement("strong");
    title.textContent = song.title;
    heading.append(title);
    if (song.melody) {
      const melody = document.createElement("small");
      melody.textContent = `Melodi: ${song.melody}`;
      heading.append(melody);
    }
    const arrow = document.createElement("span");
    arrow.className = "song-arrow";
    arrow.textContent = "⌄";
    toggle.append(number, heading, arrow);

    const content = document.createElement("div");
    content.className = "song-content";
    content.hidden = true;
    if (song.imageUrl) {
      const image = document.createElement("img");
      image.src = song.imageUrl;
      image.alt = `Illustration till ${song.title}`;
      image.loading = "lazy";
      image.decoding = "async";
      content.append(image);
    }
    const lyrics = document.createElement("p");
    lyrics.className = "song-lyrics";
    lyrics.textContent = song.lyrics;
    content.append(lyrics);

    toggle.addEventListener("click", () => {
      const willOpen = content.hidden;
      content.hidden = !willOpen;
      toggle.setAttribute("aria-expanded", `${willOpen}`);
      article.classList.toggle("open", willOpen);
    });

    article.append(toggle, content);
    songList.append(article);
  });
}

function showToast(message) {
  clearTimeout(toastTimer);
  featureMessage.textContent = message;
  featureMessage.hidden = false;
  toastTimer = setTimeout(() => { featureMessage.hidden = true; }, 2600);
}

document.querySelector("#open-admin-users").addEventListener("click", async () => {
  showView("adminUsers");
  resetParticipantForm();
  await loadPartyTables();
  await loadParticipants();
});

document.querySelector("#open-admin-stages").addEventListener("click", async () => {
  showView("adminStages");
  await loadAdminStages();
});

document.querySelector("#open-admin-challenges").addEventListener("click", async () => {
  showView("adminChallenges");
  try {
    await loadChallengeStageOptions();
    resetChallengeForm();
    await loadChallenges();
  } catch (error) {
    challengeError.textContent = error.message;
    challengeError.hidden = false;
  }
});

document.querySelector("#open-admin-songs").addEventListener("click", async () => {
  showView("adminSongs");
  resetSongForm();
  await loadAdminSongs();
});

document.querySelectorAll(".close-admin").forEach(button => {
  button.addEventListener("click", () => showView("dashboard"));
});

async function loadAdminStages() {
  adminStageList.innerHTML = '<p class="muted">Hämtar feststeg…</p>';
  adminStageError.hidden = true;
  try {
    renderAdminStages(await api.listAdminPartyStages());
  } catch (error) {
    adminStageList.replaceChildren();
    adminStageError.textContent = error.message;
    adminStageError.hidden = false;
  }
}

function renderAdminStages(stages) {
  adminStageList.replaceChildren();
  stages.forEach(stage => {
    const card = document.createElement("article");
    card.className = `stage-card${stage.isUnlocked ? " unlocked" : ""}`;
    const info = document.createElement("div");
    const heading = document.createElement("h2");
    heading.textContent = stage.displayName;
    const description = document.createElement("p");
    description.textContent = stage.description;
    const status = document.createElement("strong");
    status.className = "stage-status";
    status.textContent = stage.isUnlocked ? "Upplåst för alla" : "Låst";
    info.append(heading, description, status);

    const toggle = document.createElement("button");
    toggle.type = "button";
    toggle.className = stage.isUnlocked ? "secondary-button" : "primary-button";
    toggle.textContent = stage.isUnlocked ? "Lås igen" : "Lås upp";
    toggle.addEventListener("click", () => setPartyStage(stage, !stage.isUnlocked, toggle));
    card.append(info, toggle);
    adminStageList.append(card);
  });
}

async function setPartyStage(stage, isUnlocked, button) {
  button.disabled = true;
  adminStageError.hidden = true;
  try {
    await api.setPartyStage(stage.id, isUnlocked);
    if (stage.id === "table-reveal") setTableTileState(isUnlocked);
    await loadAdminStages();
  } catch (error) {
    adminStageError.textContent = error.message;
    adminStageError.hidden = false;
    button.disabled = false;
  }
}

async function loadAdminSongs() {
  adminSongList.innerHTML = '<p class="muted">Hämtar sånger…</p>';
  adminSongError.hidden = true;
  try {
    renderAdminSongs(await api.listSongs());
  } catch (error) {
    adminSongList.replaceChildren();
    adminSongError.textContent = error.message;
    adminSongError.hidden = false;
  }
}

function renderAdminSongs(songs) {
  adminSongList.replaceChildren();
  if (songs.length === 0) {
    adminSongList.innerHTML = '<p class="muted">Det finns inga sånger ännu.</p>';
    return;
  }

  songs.forEach(song => {
    const row = document.createElement("article");
    row.className = "admin-song-row";
    const info = document.createElement("div");
    const title = document.createElement("p");
    title.className = "participant-name";
    title.textContent = song.title;
    const detail = document.createElement("p");
    detail.className = "participant-score";
    const parts = [`Ordning ${song.sortOrder}`];
    if (song.melody) parts.push(`Melodi: ${song.melody}`);
    if (song.imageUrl) parts.push("har bild");
    detail.textContent = parts.join(" · ");
    info.append(title, detail);

    const actions = document.createElement("div");
    actions.className = "icon-actions";
    const edit = document.createElement("button");
    edit.type = "button";
    edit.className = "icon-button";
    edit.textContent = "✎";
    edit.setAttribute("aria-label", `Ändra ${song.title}`);
    edit.addEventListener("click", () => beginSongEdit(song));
    const remove = document.createElement("button");
    remove.type = "button";
    remove.className = "icon-button danger";
    remove.textContent = "🗑";
    remove.setAttribute("aria-label", `Ta bort ${song.title}`);
    remove.addEventListener("click", () => removeSong(song, remove));
    actions.append(edit, remove);
    row.append(info, actions);
    adminSongList.append(row);
  });
}

songForm.addEventListener("submit", async event => {
  event.preventDefault();
  adminSongError.hidden = true;
  adminSongStatus.hidden = true;
  saveSongButton.disabled = true;
  const imageFile = songImage.files?.[0];
  let savedSong = null;

  try {
    const sortOrder = Number.parseInt(songSortOrder.value, 10);
    savedSong = songId.value
      ? await api.updateSong(songId.value, songTitle.value, songMelody.value, songLyrics.value, sortOrder)
      : await api.createSong(songTitle.value, songMelody.value, songLyrics.value, sortOrder);

    if (imageFile) {
      adminSongStatus.textContent = "Komprimerar och laddar upp bilden…";
      adminSongStatus.hidden = false;
      const compressed = await compressImage(imageFile, 1600, 0.86);
      savedSong = await api.uploadSongImage(savedSong.id, compressed);
    }

    resetSongForm();
    adminSongStatus.textContent = "Sången är sparad.";
    adminSongStatus.hidden = false;
    await loadAdminSongs();
  } catch (error) {
    if (savedSong) {
      songId.value = savedSong.id;
      saveSongButton.textContent = "Spara ändringar";
      cancelSongEditButton.hidden = false;
      adminSongError.textContent = `Sångtexten sparades, men bilden kunde inte sparas: ${error.message}`;
      await loadAdminSongs();
    } else {
      adminSongError.textContent = error.message;
    }
    adminSongError.hidden = false;
    adminSongStatus.hidden = true;
  } finally {
    saveSongButton.disabled = false;
  }
});

cancelSongEditButton.addEventListener("click", resetSongForm);

function beginSongEdit(song) {
  currentSong = song;
  songId.value = song.id;
  songTitle.value = song.title;
  songMelody.value = song.melody;
  songLyrics.value = song.lyrics;
  songSortOrder.value = song.sortOrder;
  songImage.value = "";
  saveSongButton.textContent = "Spara ändringar";
  cancelSongEditButton.hidden = false;
  adminSongError.hidden = true;
  adminSongStatus.hidden = true;
  showCurrentSongImage(song);
  songForm.scrollIntoView({ behavior: "smooth", block: "start" });
  songTitle.focus();
}

function showCurrentSongImage(song) {
  if (song.imageUrl) {
    songCurrentImagePreview.src = song.imageUrl;
    songCurrentImage.hidden = false;
  } else {
    songCurrentImagePreview.removeAttribute("src");
    songCurrentImage.hidden = true;
  }
}

function resetSongForm() {
  songForm.reset();
  songId.value = "";
  songSortOrder.value = "0";
  currentSong = null;
  showCurrentSongImage({ imageUrl: null });
  saveSongButton.textContent = "Lägg till sång";
  cancelSongEditButton.hidden = true;
  adminSongError.hidden = true;
  adminSongStatus.hidden = true;
}

removeSongImageButton.addEventListener("click", async () => {
  if (!currentSong?.imageUrl || !window.confirm("Ta bort bilden från sången?")) return;
  removeSongImageButton.disabled = true;
  adminSongError.hidden = true;
  try {
    await api.deleteSongImage(currentSong.id);
    currentSong = { ...currentSong, imageUrl: null };
    showCurrentSongImage(currentSong);
    await loadAdminSongs();
  } catch (error) {
    adminSongError.textContent = error.message;
    adminSongError.hidden = false;
  } finally {
    removeSongImageButton.disabled = false;
  }
});

async function removeSong(song, button) {
  if (!window.confirm(`Ta bort sången ”${song.title}”?`)) return;
  button.disabled = true;
  adminSongError.hidden = true;
  try {
    await api.deleteSong(song.id);
    if (songId.value === song.id) resetSongForm();
    await loadAdminSongs();
  } catch (error) {
    adminSongError.textContent = error.message;
    adminSongError.hidden = false;
    button.disabled = false;
  }
}

async function loadPartyTables() {
  if (partyTables.length > 0) return;
  partyTables = await api.listPartyTables();
  partyTables.forEach(table => {
    const option = document.createElement("option");
    option.value = table.id;
    option.textContent = table.displayName;
    participantTable.append(option);
  });
}

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

participantForm.addEventListener("submit", async event => {
  event.preventDefault();
  adminError.hidden = true;
  saveParticipantButton.disabled = true;
  try {
    if (participantId.value) {
      await api.updateParticipant(
        participantId.value,
        participantName.value,
        participantCode.value,
        participantClueInput.value,
        participantTable.value);
    } else {
      await api.createParticipant(
        participantName.value,
        participantCode.value,
        participantClueInput.value,
        participantTable.value);
    }
    resetParticipantForm();
    await loadParticipants();
  } catch (error) {
    adminError.textContent = error.message;
    adminError.hidden = false;
  } finally {
    saveParticipantButton.disabled = false;
  }
});

participantCode.addEventListener("input", () => {
  participantCode.value = participantCode.value.toUpperCase();
});

cancelParticipantEditButton.addEventListener("click", resetParticipantForm);

function beginParticipantEdit(participant) {
  participantId.value = participant.id;
  participantName.value = participant.displayName;
  participantCode.value = participant.loginCode;
  participantClueInput.value = participant.clue;
  participantTable.value = participant.tableId;
  saveParticipantButton.textContent = "Spara ändringar";
  cancelParticipantEditButton.hidden = false;
  adminError.hidden = true;
  participantForm.scrollIntoView({ behavior: "smooth", block: "start" });
  participantName.focus();
}

function resetParticipantForm() {
  participantForm.reset();
  participantId.value = "";
  saveParticipantButton.textContent = "Lägg till deltagare";
  cancelParticipantEditButton.hidden = true;
  adminError.hidden = true;
}

async function loadPlayerChallenges() {
  playerChallengeList.innerHTML = '<p class="muted">Hämtar uppgifter…</p>';
  specialQuestionList.hidden = true;
  specialQuestionList.replaceChildren();
  playerChallengeError.hidden = true;
  try {
    const [participant, challenges, specialQuestions] = await Promise.all([
      api.getParticipant(currentParticipantId),
      api.listParticipantChallenges(currentParticipantId),
      api.listSpecialQuestions(currentParticipantId)
    ]);
    score.textContent = participant.score;
    challengeScore.textContent = participant.score;
    renderSpecialQuestions(specialQuestions);
    renderPlayerChallenges(challenges, playerChallengeList, false);
  } catch (error) {
    playerChallengeList.innerHTML = "";
    specialQuestionList.replaceChildren();
    specialQuestionList.hidden = true;
    playerChallengeError.textContent = error.message;
    playerChallengeError.hidden = false;
  }
}

function renderSpecialQuestions(questions) {
  specialQuestionList.replaceChildren();
  specialQuestionList.hidden = questions.length === 0;

  questions.forEach(question => {
    const form = document.createElement("form");
    form.className = "special-question-card";

    const label = document.createElement("label");
    label.htmlFor = `special-question-${question.id}`;
    label.textContent = question.prompt;

    const inputRow = document.createElement("div");
    inputRow.className = "percentage-input-row";
    const input = document.createElement("input");
    input.id = `special-question-${question.id}`;
    input.type = "number";
    input.min = "0";
    input.max = "100";
    input.step = "1";
    input.inputMode = "numeric";
    input.required = true;
    input.value = question.value ?? "";
    input.placeholder = "0–100";
    const suffix = document.createElement("span");
    suffix.textContent = "%";
    inputRow.append(input, suffix);

    const points = document.createElement("p");
    points.className = "special-question-points";
    const updatePreview = () => {
      const value = Number.parseInt(input.value, 10);
      points.textContent = Number.isInteger(value) && value >= 0 && value <= 100
        ? `Detta ger dig ${Math.floor(value / 10)} poäng.`
        : "Fyll i ett heltal mellan 0 och 100.";
    };
    updatePreview();
    input.addEventListener("input", updatePreview);

    const save = document.createElement("button");
    save.type = "submit";
    save.className = "primary-button";
    save.textContent = question.value === null ? "Spara svar" : "Uppdatera svar";

    const saved = document.createElement("p");
    saved.className = "upload-status";
    saved.setAttribute("role", "status");
    saved.hidden = true;

    form.addEventListener("submit", async event => {
      event.preventDefault();
      playerChallengeError.hidden = true;
      saved.hidden = true;
      save.disabled = true;
      try {
        const value = Number.parseInt(input.value, 10);
        const result = await api.setSpecialAnswer(currentParticipantId, question.id, value);
        points.textContent = `Detta ger dig ${result.points} poäng.`;
        saved.textContent = "Svaret är sparat.";
        saved.hidden = false;
        save.textContent = "Uppdatera svar";
        score.textContent = result.score;
        challengeScore.textContent = result.score;
      } catch (error) {
        playerChallengeError.textContent = error.message;
        playerChallengeError.hidden = false;
      } finally {
        save.disabled = false;
      }
    });

    form.append(label, inputRow, points, save, saved);
    specialQuestionList.append(form);
  });
}

async function loadTableChallenges() {
  tableChallengeList.innerHTML = '<p class="muted">Hämtar bordsuppgifter…</p>';
  tableChallengeError.hidden = true;
  try {
    const challenges = await api.listTableChallenges(currentParticipantId);
    setTableTileState(true);
    tableChallengeScore.textContent = challenges
      .filter(challenge => challenge.isCompleted)
      .reduce((total, challenge) => total + challenge.points, 0);
    renderPlayerChallenges(challenges, tableChallengeList, true);
  } catch (error) {
    tableChallengeList.replaceChildren();
    tableChallengeError.textContent = error.message;
    tableChallengeError.hidden = false;
    setTableTileState(false);
  }
}

function renderPlayerChallenges(challenges, target, isTableChallenge) {
  target.replaceChildren();
  if (challenges.length === 0) {
    target.innerHTML = `<p class="muted">${isTableChallenge
      ? "Inga bordsuppgifter har lagts in ännu."
      : "Inga uppgifter har lagts in ännu."}</p>`;
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
      const row = document.createElement("div");
      row.className = `completion-row${challenge.isCompleted ? " completed" : ""}`;
      const checkbox = document.createElement("input");
      checkbox.type = "checkbox";
      checkbox.id = `completion-${challenge.id}`;
      checkbox.checked = challenge.isCompleted;
      const copy = document.createElement("div");
      copy.className = "completion-copy";
      const label = document.createElement("label");
      label.htmlFor = checkbox.id;
      label.textContent = challenge.description;
      const count = document.createElement("small");
      count.className = "completion-count";
      count.textContent = completionCountText(challenge.completionCount, isTableChallenge);
      copy.append(label, count);
      checkbox.addEventListener("change", () =>
        setChallengeCompletion(challenge, checkbox, row, isTableChallenge, count));
      row.append(checkbox, copy);
      rows.append(row);
    });

    section.append(heading, rows);
    target.append(section);
  });
}

function completionCountText(count, isTableChallenge) {
  if (isTableChallenge) return count === 1 ? "1 bord klart" : `${count} bord klara`;
  return count === 1 ? "1 person klar" : `${count} personer klara`;
}

async function setChallengeCompletion(challenge, checkbox, row, isTableChallenge, countElement) {
  const requestedState = checkbox.checked;
  checkbox.disabled = true;
  const errorElement = isTableChallenge ? tableChallengeError : playerChallengeError;
  errorElement.hidden = true;
  try {
    const result = isTableChallenge
      ? await api.setTableChallengeCompletion(currentParticipantId, challenge.id, requestedState)
      : await api.setChallengeCompletion(currentParticipantId, challenge.id, requestedState);
    row.classList.toggle("completed", result.isCompleted);
    challenge.completionCount = result.completionCount;
    countElement.textContent = completionCountText(result.completionCount, isTableChallenge);
    if (isTableChallenge) {
      tableChallengeScore.textContent = result.score;
    } else {
      score.textContent = result.score;
      challengeScore.textContent = result.score;
    }
  } catch (error) {
    checkbox.checked = !requestedState;
    errorElement.textContent = error.message;
    errorElement.hidden = false;
  } finally {
    checkbox.disabled = false;
  }
}

function renderParticipants(participants) {
  participantList.replaceChildren();
  if (participants.length === 0) {
    participantList.innerHTML = '<p class="muted">Det finns inga deltagare ännu.</p>';
    return;
  }

  const groups = participants.reduce((result, participant) => {
    const group = result.get(participant.tableName) ?? [];
    group.push(participant);
    result.set(participant.tableName, group);
    return result;
  }, new Map());

  groups.forEach((tableParticipants, tableName) => {
    const section = document.createElement("section");
    const heading = document.createElement("h2");
    heading.className = "participant-group-title";
    heading.textContent = `${tableName} · ${tableParticipants.length}`;
    const rows = document.createElement("div");
    rows.className = "participant-group-rows";

    tableParticipants.forEach(participant => {
      const row = document.createElement("article");
      row.className = "participant-row";

      const info = document.createElement("div");
      const name = document.createElement("p");
      name.className = "participant-name";
      name.textContent = participant.displayName;
      const code = document.createElement("p");
      code.className = "participant-code";
      code.textContent = participant.loginCode ? `Kod: ${participant.loginCode}` : "Kod saknas";
      const clue = document.createElement("p");
      clue.className = "participant-clue";
      clue.textContent = participant.clue || "Ledtråd saknas";
      const participantScore = document.createElement("p");
      participantScore.className = "participant-score";
      participantScore.textContent = `${participant.score} poäng`;
      info.append(name, code, clue, participantScore);

      const actions = document.createElement("div");
      actions.className = "icon-actions";
      const edit = document.createElement("button");
      edit.type = "button";
      edit.className = "icon-button";
      edit.textContent = "✎";
      edit.setAttribute("aria-label", `Ändra ${participant.displayName}`);
      edit.addEventListener("click", () => beginParticipantEdit(participant));
      const remove = document.createElement("button");
      remove.type = "button";
      remove.className = "icon-button danger";
      remove.textContent = "🗑";
      remove.setAttribute("aria-label", `Ta bort ${participant.displayName}`);
      remove.addEventListener("click", () => removeParticipant(participant, remove));
      actions.append(edit, remove);
      row.append(info, actions);
      rows.append(row);
    });

    section.append(heading, rows);
    participantList.append(section);
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
    if (participantId.value === participant.id) resetParticipantForm();
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

async function loadChallengeStageOptions() {
  const stages = await api.listAdminPartyStages();
  challengeUnlockStage.replaceChildren();
  const immediate = document.createElement("option");
  immediate.value = "";
  immediate.textContent = "Direkt";
  challengeUnlockStage.append(immediate);
  stages.forEach(stage => {
    const option = document.createElement("option");
    option.value = stage.id;
    option.textContent = stage.displayName;
    challengeUnlockStage.append(option);
  });
}

function renderChallenges(challenges) {
  challengeList.replaceChildren();
  if (challenges.length === 0) {
    challengeList.innerHTML = '<p class="muted">Det finns inga poänguppgifter ännu.</p>';
    return;
  }

  [
    { id: "individual", title: "Individuella uppgifter" },
    { id: "table", title: "Bordsuppgifter" }
  ].forEach(scope => {
    const scopedChallenges = challenges.filter(challenge => challenge.scope === scope.id);
    if (scopedChallenges.length === 0) return;

    const scopeSection = document.createElement("section");
    scopeSection.className = "challenge-scope-group";
    const scopeHeading = document.createElement("h2");
    scopeHeading.className = "challenge-scope-title";
    scopeHeading.textContent = scope.title;
    scopeSection.append(scopeHeading);

    const groups = scopedChallenges.reduce((result, challenge) => {
      const items = result.get(challenge.points) ?? [];
      items.push(challenge);
      result.set(challenge.points, items);
      return result;
    }, new Map());

    [...groups.entries()].sort(([left], [right]) => left - right).forEach(([points, items]) => {
      const section = document.createElement("section");
      const heading = document.createElement("h3");
      heading.className = "challenge-group-title";
      heading.textContent = `${points} poäng`;
      const rows = document.createElement("div");
      rows.className = "challenge-items";

      items.forEach(challenge => {
        const row = document.createElement("article");
        row.className = "challenge-row";
        const info = document.createElement("div");
        const description = document.createElement("p");
        description.className = "challenge-description";
        description.textContent = challenge.description;
        info.append(description);
        if (challenge.unlockStageName) {
          const stage = document.createElement("p");
          stage.className = "challenge-stage-label";
          stage.textContent = `Visas: ${challenge.unlockStageName}`;
          info.append(stage);
        }

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
        row.append(info, actions);
        rows.append(row);
      });

      section.append(heading, rows);
      scopeSection.append(section);
    });

    challengeList.append(scopeSection);
  });
}

challengeForm.addEventListener("submit", async event => {
  event.preventDefault();
  challengeError.hidden = true;
  saveChallengeButton.disabled = true;
  try {
    const points = Number.parseInt(challengePoints.value, 10);
    if (challengeId.value) {
      await api.updateChallenge(
        challengeId.value,
        challengeDescription.value,
        points,
        challengeScope.value,
        challengeUnlockStage.value);
    } else {
      await api.createChallenge(
        challengeDescription.value,
        points,
        challengeScope.value,
        challengeUnlockStage.value);
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
  challengeScope.value = challenge.scope;
  challengeUnlockStage.value = challenge.unlockStageId ?? "";
  saveChallengeButton.textContent = "Spara ändringar";
  cancelChallengeEditButton.hidden = false;
  challengeDescription.focus();
  challengeForm.scrollIntoView({ behavior: "smooth", block: "start" });
}

function resetChallengeForm() {
  challengeForm.reset();
  challengeId.value = "";
  challengeScope.value = "individual";
  challengeUnlockStage.value = "";
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
