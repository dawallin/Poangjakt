async function request(path, options) {
  const response = await fetch(path, options);
  if (response.ok) {
    return response.status === 204 ? null : response.json();
  }

  let problem;
  try { problem = await response.json(); } catch { problem = null; }
  const validationMessage = problem?.errors
    ? Object.values(problem.errors).flat()[0]
    : null;
  throw new Error(validationMessage ?? problem?.title ?? `Servern svarade ${response.status}.`);
}

export const api = {
  async signInAdmin(secret) {
    const response = await fetch("/api/admin-session", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ secret })
    });
    if (response.status === 401) return null;
    if (!response.ok) throw new Error(`Servern svarade ${response.status}.`);
    return response.json();
  },
  async getAdminSession() {
    const response = await fetch("/api/admin-session");
    if (response.status === 401) return null;
    if (!response.ok) throw new Error(`Servern svarade ${response.status}.`);
    return response.json();
  },
  signOutAdmin() { return request("/api/admin-session", { method: "DELETE" }); },
  async loginParticipant(code) {
    const response = await fetch("/api/participants/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ code })
    });
    if (response.status === 401) throw new Error("Koden stämmer inte. Kontrollera kortet och försök igen.");
    if (!response.ok) throw new Error(`Servern svarade ${response.status}.`);
    return response.json();
  },
  getParticipant(id) { return request(`/api/participants/${encodeURIComponent(id)}`); },
  listPartyStages() { return request("/api/party-stages"); },
  getParticipantTable(participantId) {
    return request(`/api/participants/${encodeURIComponent(participantId)}/table`);
  },
  listLeaderboard() { return request("/api/participants"); },
  listParticipantChallenges(participantId) {
    return request(`/api/participants/${encodeURIComponent(participantId)}/challenges`);
  },
  setChallengeCompletion(participantId, challengeId, isCompleted) {
    return request(
      `/api/participants/${encodeURIComponent(participantId)}/challenges/${encodeURIComponent(challengeId)}`,
      {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ isCompleted })
      });
  },
  listTableChallenges(participantId) {
    return request(`/api/participants/${encodeURIComponent(participantId)}/table-challenges`);
  },
  setTableChallengeCompletion(participantId, challengeId, isCompleted) {
    return request(
      `/api/participants/${encodeURIComponent(participantId)}/table-challenges/${encodeURIComponent(challengeId)}`,
      {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ isCompleted })
      });
  },
  listTableLeaderboard(participantId) {
    return request(`/api/participants/${encodeURIComponent(participantId)}/table-leaderboard`);
  },
  listParticipants() { return request("/api/admin/participants"); },
  listPartyTables() { return request("/api/admin/party-tables"); },
  listAdminPartyStages() { return request("/api/admin/party-stages"); },
  setPartyStage(id, isUnlocked) {
    return request(`/api/admin/party-stages/${encodeURIComponent(id)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ isUnlocked })
    });
  },
  createParticipant(displayName, loginCode, clue, tableId) {
    return request("/api/admin/participants", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ displayName, loginCode, clue, tableId })
    });
  },
  updateParticipant(id, displayName, loginCode, clue, tableId) {
    return request(`/api/admin/participants/${encodeURIComponent(id)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ displayName, loginCode, clue, tableId })
    });
  },
  deleteParticipant(id) {
    return request(`/api/admin/participants/${encodeURIComponent(id)}`, { method: "DELETE" });
  },
  listChallenges() { return request("/api/admin/challenges"); },
  createChallenge(description, points, scope) {
    return request("/api/admin/challenges", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ description, points, scope })
    });
  },
  updateChallenge(id, description, points, scope) {
    return request(`/api/admin/challenges/${encodeURIComponent(id)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ description, points, scope })
    });
  },
  deleteChallenge(id) {
    return request(`/api/admin/challenges/${encodeURIComponent(id)}`, { method: "DELETE" });
  },
  listPhotos() { return request("/api/photos"); },
  uploadPhoto(participantId, image, thumbnail) {
    const form = new FormData();
    form.append("participantId", participantId);
    form.append("image", image, "image.jpg");
    form.append("thumbnail", thumbnail, "thumbnail.jpg");
    return request("/api/photos", { method: "POST", body: form });
  },
  deletePhoto(id) {
    return request(`/api/admin/photos/${encodeURIComponent(id)}`, { method: "DELETE" });
  },
  deleteOwnPhoto(participantId, id) {
    return request(
      `/api/participants/${encodeURIComponent(participantId)}/photos/${encodeURIComponent(id)}`,
      { method: "DELETE" });
  },
  listSongs() { return request("/api/songs"); },
  createSong(title, melody, lyrics, sortOrder) {
    return request("/api/admin/songs", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title, melody, lyrics, sortOrder })
    });
  },
  updateSong(id, title, melody, lyrics, sortOrder) {
    return request(`/api/admin/songs/${encodeURIComponent(id)}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title, melody, lyrics, sortOrder })
    });
  },
  uploadSongImage(id, image) {
    const form = new FormData();
    form.append("image", image, "song-image.jpg");
    return request(`/api/admin/songs/${encodeURIComponent(id)}/image`, { method: "POST", body: form });
  },
  deleteSongImage(id) {
    return request(`/api/admin/songs/${encodeURIComponent(id)}/image`, { method: "DELETE" });
  },
  deleteSong(id) {
    return request(`/api/admin/songs/${encodeURIComponent(id)}`, { method: "DELETE" });
  },
  testStorage() { return request("/health/storage", { method: "POST" }); }
};
