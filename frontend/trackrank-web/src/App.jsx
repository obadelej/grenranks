import { useEffect, useState } from "react";
import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import LookupForms from "./components/LookupForms";
import ResultForm from "./components/ResultForm";
import ResultsTable from "./components/ResultsTable";
import RankingsTable from "./components/RankingsTable";
import MissingDobFixer from "./components/MissingDobFixer";
import HytekImportSection from "./components/HytekImportSection";
import {
  createResult,
  createAthlete,
  createEvent,
  createMeet,
  deleteResult as deleteResultById,
  fetchMissingDobAthletes,
  fetchRankings,
  fetchLookups,
  getStoredAdminKey,
  loginAsAdmin,
  logoutAdmin,
  fetchResults,
  seedData as seedDataApi,
  updateAthleteDob,
  updateResult,
} from "./api/resultsapi";
import { readResultsUrlState, writeResultsUrlState } from "./utils/urlSync";

const initialForm = {
  athleteId: "",
  meetId: "",
  eventId: "",
  performance: "",
  wind: "",
  resultDate: new Date().toISOString().split("T")[0],
};

const rankingCategories = ["U7", "U9", "U11", "U13", "U15", "U17", "U20", "20 Plus"];
const rankingGenders = ["Male", "Female"];
const rankingYears = ["", "2026", "2025", "2024", "2023", "2022", "2021", "2020"];
const resultSortOptions = [
  { value: "resultDate", label: "Result Date" },
  { value: "performance", label: "Performance" },
  { value: "createdAtUtc", label: "Created At" },
  { value: "athleteName", label: "Athlete Name" },
  { value: "eventName", label: "Event Name" },
];
const sourceTypeOptions = ["", "Manual", "HytekImport"];

function App() {
  const [form, setForm] = useState(initialForm);
  const [results, setResults] = useState([]);
  const [resultsPage, setResultsPage] = useState(1);
  const [resultsPageSize] = useState(25);
  const [resultsTotalCount, setResultsTotalCount] = useState(0);
  const [resultsFilters, setResultsFilters] = useState({
    athleteId: "",
    eventId: "",
    year: "",
    sourceType: "",
    sortBy: "resultDate",
    sortDir: "desc",
  });
  const [athletes, setAthletes] = useState([]);
  const [meets, setMeets] = useState([]);
  const [events, setEvents] = useState([]);
  const [message, setMessage] = useState("");
  const [loading, setLoading] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [selectedRankingEventId, setSelectedRankingEventId] = useState("");
  const [selectedRankingGender, setSelectedRankingGender] = useState("Male");
  const [selectedRankingCategory, setSelectedRankingCategory] = useState("U7");
  const [selectedRankingYear, setSelectedRankingYear] = useState(String(new Date().getFullYear()));
  const [bestPerAthleteOnly, setBestPerAthleteOnly] = useState(true);
  const [rankingsLoading, setRankingsLoading] = useState(false);
  const [rankingsData, setRankingsData] = useState(null);
  const [missingDobAthletes, setMissingDobAthletes] = useState([]);
  const [isAdmin, setIsAdmin] = useState(false);
  const [adminKeyInput, setAdminKeyInput] = useState("");

  async function loadLookups() {
    try {
      const { athletes: athletesData, meets: meetsData, events: eventsData } =
        await fetchLookups();

      setAthletes(athletesData);
      setMeets(meetsData);
      setEvents(eventsData);

      setForm((prev) => ({
        ...prev,
        athleteId: prev.athleteId || athletesData[0]?.id?.toString() || "",
        meetId: prev.meetId || meetsData[0]?.id?.toString() || "",
        eventId: prev.eventId || eventsData[0]?.id?.toString() || "",
      }));
      setSelectedRankingEventId((prev) => prev || eventsData[0]?.id?.toString() || "");
    } catch (err) {
      setMessage(err.message);
    }
  }

  async function loadResults(page = resultsPage, filters = resultsFilters) {
    try {
      const data = await fetchResults({
        page,
        pageSize: resultsPageSize,
        athleteId: filters.athleteId ? Number(filters.athleteId) : undefined,
        eventId: filters.eventId ? Number(filters.eventId) : undefined,
        year: filters.year ? Number(filters.year) : undefined,
        sourceType: filters.sourceType || undefined,
        sortBy: filters.sortBy,
        sortDir: filters.sortDir,
      });
      setResults(data.items ?? []);
      setResultsTotalCount(data.totalCount ?? 0);
      const nextPage = data.page ?? page;
      setResultsPage(nextPage);
      writeResultsUrlState({ page: nextPage, filters });
    } catch (err) {
      setMessage(err.message);
    }
  }

  useEffect(() => {
    const storedKey = getStoredAdminKey();
    if (!storedKey) return;
    void (async () => {
      try {
        await loginAsAdmin(storedKey);
        setIsAdmin(true);
      } catch {
        logoutAdmin();
        setIsAdmin(false);
      }
    })();
  }, []);

  useEffect(() => {
    const fromUrl = readResultsUrlState();
    setResultsFilters(fromUrl.filters);
    setResultsPage(fromUrl.page);

    async function init() {
      await loadLookups();
      await loadResults(fromUrl.page, fromUrl.filters);
    }
    void init();

    function onPopState() {
      const next = readResultsUrlState();
      setResultsFilters(next.filters);
      setResultsPage(next.page);
      void loadResults(next.page, next.filters);
    }
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
  }, []);

  function onChange(e) {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
  }

  function onResultsFilterChange(e) {
    const { name, value } = e.target;
    setResultsFilters((prev) => ({ ...prev, [name]: value }));
  }

  async function applyResultsFilters() {
    await loadResults(1, resultsFilters);
  }

  function resetForm() {
    setEditingId(null);
    setForm((prev) => ({
      ...initialForm,
      athleteId: prev.athleteId || athletes[0]?.id?.toString() || "",
      meetId: prev.meetId || meets[0]?.id?.toString() || "",
      eventId: prev.eventId || events[0]?.id?.toString() || "",
    }));
  }

  async function seedData() {
    setMessage("");
    try {
      await seedDataApi();
      await loadLookups();
      await loadResults(resultsPage, resultsFilters);
      setMessage("Seed successful. Dropdown options updated.");
    } catch (err) {
      setMessage(err.message);
    }
  }

  async function addAthlete(payload) {
    setMessage("");
    try {
      const normalizedFirst = payload.firstName.trim().toLowerCase();
      const normalizedLast = payload.lastName.trim().toLowerCase();
      const duplicateAthlete = athletes.find(
        (a) =>
          String(a.firstName || "").trim().toLowerCase() === normalizedFirst &&
          String(a.lastName || "").trim().toLowerCase() === normalizedLast,
      );
      if (duplicateAthlete) {
        setForm((prev) => ({ ...prev, athleteId: String(duplicateAthlete.id) }));
        setMessage(
          `Athlete already exists: ${duplicateAthlete.firstName} ${duplicateAthlete.lastName}. Selected existing athlete.`,
        );
        return;
      }

      const created = await createAthlete(payload);
      await loadLookups();
      setForm((prev) => ({ ...prev, athleteId: String(created.id) }));
      setMessage(`Athlete added: ${created.firstName} ${created.lastName}`);
    } catch (err) {
      setMessage(err.message);
    }
  }

  async function addMeet(payload) {
    setMessage("");
    try {
      const normalizedMeetName = payload.name.trim().toLowerCase();
      const duplicateMeet = meets.find(
        (m) => String(m.name || "").trim().toLowerCase() === normalizedMeetName,
      );
      if (duplicateMeet) {
        setForm((prev) => ({ ...prev, meetId: String(duplicateMeet.id) }));
        setMessage(`Meet already exists: ${duplicateMeet.name}. Selected existing meet.`);
        return;
      }

      const created = await createMeet(payload);
      await loadLookups();
      setForm((prev) => ({ ...prev, meetId: String(created.id) }));
      setMessage(`Meet added: ${created.name}`);
    } catch (err) {
      setMessage(err.message);
    }
  }

  async function addEvent(payload) {
    setMessage("");
    try {
      const normalizedEventName = payload.name.trim().toLowerCase();
      const duplicateEvent = events.find(
        (ev) => String(ev.name || "").trim().toLowerCase() === normalizedEventName,
      );
      if (duplicateEvent) {
        setForm((prev) => ({ ...prev, eventId: String(duplicateEvent.id) }));
        setMessage(`Event already exists: ${duplicateEvent.name}. Selected existing event.`);
        return;
      }

      const created = await createEvent(payload);
      await loadLookups();
      setForm((prev) => ({ ...prev, eventId: String(created.id) }));
      setMessage(`Event added: ${created.name}`);
    } catch (err) {
      setMessage(err.message);
    }
  }

  async function submitResult(e) {
    e.preventDefault();
    setLoading(true);
    setMessage("");

    try {
      const payload = {
        athleteId: Number(form.athleteId),
        meetId: Number(form.meetId),
        eventId: Number(form.eventId),
        performance: Number(form.performance),
        wind: form.wind === "" ? null : Number(form.wind),
        resultDate: new Date(form.resultDate).toISOString(),
      };

      if (editingId === null) {
        await createResult(payload);
      } else {
        await updateResult(editingId, payload);
      }

      setMessage(editingId === null ? "Result saved." : "Result updated.");
      resetForm();
      await loadResults(resultsPage, resultsFilters);
    } catch (err) {
      setMessage(err.message);
    } finally {
      setLoading(false);
    }
  }

  function startEdit(result) {
    setEditingId(result.id);
    setForm({
      athleteId: String(result.athleteId),
      meetId: String(result.meetId),
      eventId: String(result.eventId),
      performance: String(result.performance),
      wind: result.wind === null ? "" : String(result.wind),
      resultDate: new Date(result.resultDate).toISOString().split("T")[0],
    });
    setMessage(`Editing result #${result.id}`);
  }

  async function deleteResult(id) {
    const ok = window.confirm("Delete this result?");
    if (!ok) return;

    setMessage("");
    try {
      await deleteResultById(id);

      if (editingId === id) {
        resetForm();
      }

      setMessage("Result deleted.");
      await loadResults(resultsPage, resultsFilters);
    } catch (err) {
      setMessage(err.message);
    }
  }

  async function loadRankings() {
    setMessage("");
    setRankingsData(null);
    setRankingsLoading(true);

    try {
      if (!selectedRankingEventId) {
        throw new Error("Select an event to load rankings.");
      }

      const data = await fetchRankings({
        eventId: Number(selectedRankingEventId),
        gender: selectedRankingGender,
        category: selectedRankingCategory,
        year: selectedRankingYear ? Number(selectedRankingYear) : undefined,
        bestPerAthleteOnly,
      });
      setRankingsData(data);

      if (data.missingDobCount > 0) {
        const missing = await fetchMissingDobAthletes({
          eventId: Number(selectedRankingEventId),
          gender: selectedRankingGender,
        });
        setMissingDobAthletes(missing);
      } else {
        setMissingDobAthletes([]);
      }
    } catch (err) {
      setMessage(err.message);
    } finally {
      setRankingsLoading(false);
    }
  }

  async function fixAthleteDob(athleteId, dateOfBirth) {
    try {
      await updateAthleteDob(athleteId, dateOfBirth);
      setMessage("Athlete birthdate updated.");
      await loadRankings();
    } catch (err) {
      setMessage(err.message);
    }
  }

  async function handleAdminLogin(e) {
    e.preventDefault();
    setMessage("");
    try {
      const result = await loginAsAdmin(adminKeyInput.trim());
      setIsAdmin(true);
      setAdminKeyInput("");
      setMessage(result.warning || "Admin login successful.");
    } catch (err) {
      setMessage(err.message);
      setIsAdmin(false);
    }
  }

  function handleAdminLogout() {
    logoutAdmin();
    setIsAdmin(false);
    setAdminKeyInput("");
    setMessage("Logged out of admin mode.");
  }

  return (
    <div className="app-shell">
      <h1>Grenada Track &amp; Field Rankings</h1>
      <p className="app-subtitle">Manual result entry, imports, and rankings</p>
      <nav className="top-nav">
        <NavLink to="/rankings" className={({ isActive }) => `top-nav-link${isActive ? " active" : ""}`}>
          Rankings
        </NavLink>
        {isAdmin && (
          <>
            <NavLink to="/manual-entry" className={({ isActive }) => `top-nav-link${isActive ? " active" : ""}`}>
              Manual Entry
            </NavLink>
            <NavLink to="/results" className={({ isActive }) => `top-nav-link${isActive ? " active" : ""}`}>
              Results
            </NavLink>
          </>
        )}
      </nav>
      <div className="card">
        {isAdmin ? (
          <div className="row-wrap">
            <b>Admin mode enabled</b>
            <button type="button" onClick={handleAdminLogout}>Log out</button>
          </div>
        ) : (
          <form onSubmit={handleAdminLogin} className="row-wrap">
            <input
              type="password"
              value={adminKeyInput}
              onChange={(e) => setAdminKeyInput(e.target.value)}
              placeholder="Admin key"
              autoComplete="current-password"
              required
            />
            <button type="submit">Admin login</button>
            <span className="muted-text">Only Rankings is public without login.</span>
          </form>
        )}
      </div>

      <Routes>
        <Route path="/" element={<Navigate to="/rankings" replace />} />
        <Route
          path="/manual-entry"
          element={isAdmin ? (
            <>
              <LookupForms
                seedData={seedData}
                athletes={athletes}
                meets={meets}
                events={events}
                form={form}
                onChange={onChange}
                onCreateAthlete={addAthlete}
                onCreateMeet={addMeet}
                onCreateEvent={addEvent}
              />
              <ResultForm
                form={form}
                onChange={onChange}
                onSubmit={submitResult}
                loading={loading}
                editingId={editingId}
                onCancelEdit={resetForm}
              />
              {message && <p className="status-message"><b>Status:</b> {message}</p>}
              <HytekImportSection
                onImportSuccess={async () => {
                  await loadLookups();
                  await loadResults(1, resultsFilters);
                }}
              />
            </>
          ) : <Navigate to="/rankings" replace />}
        />
        <Route
          path="/results"
          element={isAdmin ? (
            <>
              {message && <p className="status-message"><b>Status:</b> {message}</p>}
              <ResultsTable results={results} onEdit={startEdit} onDelete={deleteResult} />
              <div className="card">
                <b>Results Filters</b>
                <div className="row-wrap">
                  <select name="athleteId" value={resultsFilters.athleteId} onChange={onResultsFilterChange}>
                    <option value="">All athletes</option>
                    {athletes.map((a) => (
                      <option key={a.id} value={a.id}>{a.name}</option>
                    ))}
                  </select>
                  <select name="eventId" value={resultsFilters.eventId} onChange={onResultsFilterChange}>
                    <option value="">All events</option>
                    {events.map((ev) => (
                      <option key={ev.id} value={ev.id}>{ev.name}</option>
                    ))}
                  </select>
                  <input
                    name="year"
                    placeholder="Year"
                    value={resultsFilters.year}
                    onChange={onResultsFilterChange}
                    className="year-input"
                  />
                  <select name="sourceType" value={resultsFilters.sourceType} onChange={onResultsFilterChange}>
                    {sourceTypeOptions.map((s) => (
                      <option key={s || "all"} value={s}>{s || "All sources"}</option>
                    ))}
                  </select>
                  <select name="sortBy" value={resultsFilters.sortBy} onChange={onResultsFilterChange}>
                    {resultSortOptions.map((s) => (
                      <option key={s.value} value={s.value}>{s.label}</option>
                    ))}
                  </select>
                  <select name="sortDir" value={resultsFilters.sortDir} onChange={onResultsFilterChange}>
                    <option value="desc">Desc</option>
                    <option value="asc">Asc</option>
                  </select>
                  <button onClick={applyResultsFilters}>Apply</button>
                </div>
              </div>
              <div className="pagination-row">
                <button
                  onClick={() => loadResults(resultsPage - 1, resultsFilters)}
                  disabled={resultsPage <= 1}
                >
                  Previous
                </button>
                <span>
                  Page {resultsPage} of {Math.max(1, Math.ceil(resultsTotalCount / resultsPageSize))}
                </span>
                <button
                  onClick={() => loadResults(resultsPage + 1, resultsFilters)}
                  disabled={resultsPage >= Math.ceil(resultsTotalCount / resultsPageSize)}
                >
                  Next
                </button>
                <span className="muted-text">Total results: {resultsTotalCount}</span>
              </div>
            </>
          ) : <Navigate to="/rankings" replace />}
        />
        <Route
          path="/rankings"
          element={
            <>
              <div className="section">
                <h2>Rankings</h2>
                <div className="row-wrap">
                  <select
                    value={selectedRankingEventId}
                    onChange={(e) => setSelectedRankingEventId(e.target.value)}
                  >
                    <option value="" disabled>
                      Select event
                    </option>
                    {events.map((ev) => (
                      <option key={ev.id} value={ev.id}>
                        {ev.name}
                      </option>
                    ))}
                  </select>
                  <button onClick={loadRankings}>Load Rankings</button>
                </div>
                <div className="row-wrap">
                  <select
                    value={selectedRankingGender}
                    onChange={(e) => setSelectedRankingGender(e.target.value)}
                  >
                    {rankingGenders.map((g) => (
                      <option key={g} value={g}>
                        {g}
                      </option>
                    ))}
                  </select>

                  <select
                    value={selectedRankingCategory}
                    onChange={(e) => setSelectedRankingCategory(e.target.value)}
                  >
                    {rankingCategories.map((c) => (
                      <option key={c} value={c}>
                        {c}
                      </option>
                    ))}
                  </select>
                  <select
                    value={selectedRankingYear}
                    onChange={(e) => setSelectedRankingYear(e.target.value)}
                  >
                    {rankingYears.map((yearValue) => (
                      <option key={yearValue || "all"} value={yearValue}>
                        {yearValue || "All years"}
                      </option>
                    ))}
                  </select>
                  <label className="inline-checkbox">
                    <input
                      type="checkbox"
                      checked={bestPerAthleteOnly}
                      onChange={(e) => setBestPerAthleteOnly(e.target.checked)}
                    />
                    Best per athlete only
                  </label>
                </div>
              </div>

              {rankingsLoading && <p>Loading rankings...</p>}
              {message && <p className="status-message"><b>Status:</b> {message}</p>}
              <RankingsTable rankingsData={rankingsData} />
              <MissingDobFixer athletes={missingDobAthletes} onSaveDob={fixAthleteDob} />
            </>
          }
        />
      </Routes>
    </div>
  );
}

export default App;