async function request(path, options) {
  const response = await fetch(path, options);
  if (response.ok) {
    return response.status === 204 ? null : response.json();
  }

  let problem;
  try { problem = await response.json(); } catch { problem = null; }
  const validationMessage = problem?.errors?.DisplayName?.[0] ?? problem?.errors?.displayName?.[0];
  throw new Error(validationMessage ?? problem?.title ?? `Servern svarade ${response.status}.`);
}

export const api = {
  registerParticipant(displayName) {
    return request("/api/participants/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ displayName })
    });
  },
  getParticipant(id) { return request(`/api/participants/${encodeURIComponent(id)}`); },
  listParticipants() { return request("/api/admin/participants"); },
  deleteParticipant(id) {
    return request(`/api/admin/participants/${encodeURIComponent(id)}`, { method: "DELETE" });
  },
  testStorage() { return request("/health/storage", { method: "POST" }); }
};
