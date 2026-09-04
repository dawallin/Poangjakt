import { api } from "./api-client.js";
import { compressImage, compressPhoto } from "./image-utils.js";

const participantKey = "poangjakten.participantId";
const views = {
  loading: document.querySelector("#loading-view"),
  registration: document.querySelector("#registration-view"),
  dashboard: document.querySelector("#dashboard-view"),
  challenges: document.querySelector("#challenges-view"),
  photos: document.querySelector("#photos-view"),
  songs: document.querySelector("#songs-view"),
  adminUsers: document.querySelector("#admin-users-view"),
  adminChallenges: document.querySelector("#admin-challenges-view"),
  adminSongs: document.querySelector("#admin-songs-view")
};
const registrationForm = document.querySelector("#registration-form");
const displayNameInput = document.querySelector("#display-name");
const registrationError = document.querySelector("#registration-error");
const welcomeHeading = document.querySelector("#welcome-heading");
const score = document.querySelector("#score");
const scoreBadge = document.querySelector("#score-badge");
const challengeScore = document.querySelector("#challenge-score");
const playerChallengeList = document.querySelector("#player-challenge-list");
const playerChallengeError = document.querySelector("#player-challenge-error");
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
let toastTimer;
let currentParticipantId = null;
let currentIsAdmin = false;
let currentSong = null;

function showView(name) {
  Object.entries(views).forEach(([viewName, element]) => { element.hidden = viewName !== name; });
  window.scrollTo({ top: 0, behavior: "instant" });
}

function showParticipant(participant) {
  currentParticipantId = participant.id;
  currentIsAdmin = false;
  welcomeHeading.textContent = `Hej, ${participant.displayName}!`;
  score.textContent = participant.score;
  scoreBadge.hidden = false;
  document.querySelectorAll(".admin-only").forEach(element => { element.hidden = true; });
  document.querySelectorAll(".participant-only").forEach(element => { element.hidden = false; });
  showView("dashboard");
}

function showAdmin(session) {
  currentParticipantId = null;
  currentIsAdmin = true;
  welcomeHeading.textContent = `Hej, ${session.displayName}!`;
  scoreBadge.hidden = true;
  document.querySelectorAll(".admin-only").forEach(element => { element.hidden = false; });
  document.querySelectorAll(".participant-only").forEach(element => { element.hidden = true; });
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
    showToast("Den här delen öppnar vi i nästa steg.");
  });
});

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

    if (currentIsAdmin) {
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
    await api.deletePhoto(photo.id);
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
  await loadParticipants();
});

document.querySelector("#open-admin-challenges").addEventListener("click", async () => {
  showView("adminChallenges");
  resetChallengeForm();
  await loadChallenges();
});

document.querySelector("#open-admin-songs").addEventListener("click", async () => {
  showView("adminSongs");
  resetSongForm();
  await loadAdminSongs();
});

document.querySelectorAll(".close-admin").forEach(button => {
  button.addEventListener("click", () => showView("dashboard"));
});

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

async function loadPlayerChallenges() {
  playerChallengeList.innerHTML = '<p class="muted">Hämtar uppgifter…</p>';
  playerChallengeError.hidden = true;
  try {
    renderPlayerChallenges(await api.listParticipantChallenges(currentParticipantId));
  } catch (error) {
    playerChallengeList.innerHTML = "";
    playerChallengeError.textContent = error.message;
    playerChallengeError.hidden = false;
  }
}

function renderPlayerChallenges(challenges) {
  playerChallengeList.replaceChildren();
  if (challenges.length === 0) {
    playerChallengeList.innerHTML = '<p class="muted">Inga uppgifter har lagts in ännu.</p>';
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
      const label = document.createElement("label");
      label.htmlFor = checkbox.id;
      label.textContent = challenge.description;
      checkbox.addEventListener("change", () => setChallengeCompletion(challenge, checkbox, row));
      row.append(checkbox, label);
      rows.append(row);
    });

    section.append(heading, rows);
    playerChallengeList.append(section);
  });
}

async function setChallengeCompletion(challenge, checkbox, row) {
  const requestedState = checkbox.checked;
  checkbox.disabled = true;
  playerChallengeError.hidden = true;
  try {
    const result = await api.setChallengeCompletion(currentParticipantId, challenge.id, requestedState);
    row.classList.toggle("completed", result.isCompleted);
    score.textContent = result.score;
    challengeScore.textContent = result.score;
  } catch (error) {
    checkbox.checked = !requestedState;
    playerChallengeError.textContent = error.message;
    playerChallengeError.hidden = false;
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
