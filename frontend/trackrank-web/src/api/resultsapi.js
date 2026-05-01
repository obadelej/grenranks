const API_BASE = import.meta.env.VITE_API_BASE_URL || "http://localhost:5002";
const ADMIN_KEY_STORAGE_KEY = "trackrank_admin_key";

let adminApiKey = "";
if (typeof window !== "undefined") {
  adminApiKey = window.localStorage.getItem(ADMIN_KEY_STORAGE_KEY) || "";
}

function withAdminHeaders(headers = {}) {
  if (!adminApiKey) return headers;
  return {
    ...headers,
    "X-Admin-Key": adminApiKey,
  };
}

async function parseResponse(response, fallbackMessage) {
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || fallbackMessage);
  }
  return response;
}

export async function fetchLookups() {
  const [athletesRes, meetsRes, eventsRes] = await Promise.all([
    fetch(`${API_BASE}/api/lookups/athletes`),
    fetch(`${API_BASE}/api/lookups/meets`),
    fetch(`${API_BASE}/api/lookups/events`),
  ]);

  await parseResponse(athletesRes, "Failed to load athletes");
  await parseResponse(meetsRes, "Failed to load meets");
  await parseResponse(eventsRes, "Failed to load events");

  const [athletes, meets, events] = await Promise.all([
    athletesRes.json(),
    meetsRes.json(),
    eventsRes.json(),
  ]);

  return { athletes, meets, events };
}

export async function fetchResults({
  page = 1,
  pageSize = 25,
  athleteId,
  eventId,
  year,
  sourceType,
  sortBy = "resultDate",
  sortDir = "desc",
} = {}) {
  const query = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
    sortBy,
    sortDir,
  });
  if (athleteId) query.set("athleteId", String(athleteId));
  if (eventId) query.set("eventId", String(eventId));
  if (year) query.set("year", String(year));
  if (sourceType) query.set("sourceType", String(sourceType));
  const response = await fetch(`${API_BASE}/api/results?${query.toString()}`);
  await parseResponse(response, "Failed to load results");
  return response.json();
}

export async function seedData() {
  const response = await fetch(`${API_BASE}/api/seed`, {
    method: "POST",
    headers: withAdminHeaders(),
  });
  await parseResponse(response, "Seed failed");
}

export async function createResult(payload) {
  const response = await fetch(`${API_BASE}/api/results`, {
    method: "POST",
    headers: withAdminHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify(payload),
  });
  await parseResponse(response, "Create failed");
  return response.json();
}

export async function createAthlete(payload) {
  const response = await fetch(`${API_BASE}/api/athletes`, {
    method: "POST",
    headers: withAdminHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify(payload),
  });
  await parseResponse(response, "Create athlete failed");
  return response.json();
}

export async function createMeet(payload) {
  const response = await fetch(`${API_BASE}/api/meets`, {
    method: "POST",
    headers: withAdminHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify(payload),
  });
  await parseResponse(response, "Create meet failed");
  return response.json();
}

export async function createEvent(payload) {
  const response = await fetch(`${API_BASE}/api/events`, {
    method: "POST",
    headers: withAdminHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify(payload),
  });
  await parseResponse(response, "Create event failed");
  return response.json();
}

export async function updateResult(id, payload) {
  const response = await fetch(`${API_BASE}/api/results/${id}`, {
    method: "PUT",
    headers: withAdminHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify(payload),
  });
  await parseResponse(response, "Update failed");
  return response.json();
}

export async function deleteResult(id) {
  const response = await fetch(`${API_BASE}/api/results/${id}`, {
    method: "DELETE",
    headers: withAdminHeaders(),
  });
  await parseResponse(response, "Delete failed");
}

export async function fetchRankings({
  eventId,
  gender,
  category,
  year,
  bestPerAthleteOnly,
}) {
  const query = new URLSearchParams({
    eventId: String(eventId),
    gender,
    category,
    bestPerAthleteOnly: String(Boolean(bestPerAthleteOnly)),
  });
  if (year) {
    query.set("year", String(year));
  }
  const response = await fetch(`${API_BASE}/api/rankings?${query.toString()}`);
  await parseResponse(response, "Failed to load rankings");
  return response.json();
}

export async function fetchMissingDobAthletes({ eventId, gender }) {
  const query = new URLSearchParams({
    eventId: String(eventId),
    gender,
  });
  const response = await fetch(`${API_BASE}/api/athletes/missing-dob?${query.toString()}`);
  await parseResponse(response, "Failed to load athletes missing DOB");
  return response.json();
}

export async function updateAthleteDob(id, dateOfBirth) {
  const response = await fetch(`${API_BASE}/api/athletes/${id}/date-of-birth`, {
    method: "PUT",
    headers: withAdminHeaders({ "Content-Type": "application/json" }),
    body: JSON.stringify({ dateOfBirth: new Date(dateOfBirth).toISOString() }),
  });
  await parseResponse(response, "Failed to update athlete DOB");
  return response.json();
}

export async function fetchImportHistory({ page = 1, pageSize = 10 } = {}) {
  const query = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
  });
  const response = await fetch(
    `${API_BASE}/api/imports/history?${query.toString()}`,
  );
  await parseResponse(response, "Failed to load import history");
  return response.json();
}

export async function importHytekCsv(file) {
  const formData = new FormData();
  formData.append("File", file);
  const response = await fetch(`${API_BASE}/api/imports/hytek`, {
    method: "POST",
    headers: withAdminHeaders(),
    body: formData,
  });
  await parseResponse(response, "Hy-Tek import failed");
  return response.json();
}

export async function loginAsAdmin(apiKey) {
  const response = await fetch(`${API_BASE}/api/auth/admin-login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ apiKey }),
  });
  await parseResponse(response, "Admin login failed");
  const data = await response.json();
  adminApiKey = apiKey;
  if (typeof window !== "undefined") {
    window.localStorage.setItem(ADMIN_KEY_STORAGE_KEY, apiKey);
  }
  return data;
}

export function logoutAdmin() {
  adminApiKey = "";
  if (typeof window !== "undefined") {
    window.localStorage.removeItem(ADMIN_KEY_STORAGE_KEY);
  }
}

export function getStoredAdminKey() {
  return adminApiKey;
}
