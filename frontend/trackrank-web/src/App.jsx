import { useEffect, useState } from "react";
import LookupForms from "./components/LookupForms";
import ResultForm from "./components/ResultForm";
import ResultsTable from "./components/ResultsTable";
import RankingsTable from "./components/RankingsTable";
import MissingDobFixer from "./components/MissingDobFixer";
import HytekImportSection from "./components/HytekImportSection";
import {
  createResult,
  deleteResult as deleteResultById,
  fetchMissingDobAthletes,
  fetchRankings,
  fetchLookups,
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

  return (
    <div style={{ maxWidth: 1000, margin: "20px auto", fontFamily: "Arial, sans-serif" }}>
      <h1>TrackRank - Manual Result Entry</h1>

      <LookupForms
        seedData={seedData}
        athletes={athletes}
        meets={meets}
        events={events}
        form={form}
        onChange={onChange}
      />

      <ResultForm
        form={form}
        onChange={onChange}
        onSubmit={submitResult}
        loading={loading}
        editingId={editingId}
        onCancelEdit={resetForm}
      />

      {message && <p><b>Status:</b> {message}</p>}

      <ResultsTable results={results} onEdit={startEdit} onDelete={deleteResult} />
      <div style={{ marginTop: 10, padding: 10, border: "1px solid #ddd" }}>
        <b>Results Filters</b>
        <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginTop: 8 }}>
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
            style={{ width: 90 }}
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
      <div style={{ display: "flex", gap: 8, alignItems: "center", marginTop: 8 }}>
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
        <span style={{ color: "#555" }}>Total results: {resultsTotalCount}</span>
      </div>

      <HytekImportSection
        onImportSuccess={async () => {
          await loadLookups();
          await loadResults(1, resultsFilters);
        }}
      />

      <div style={{ marginTop: 24 }}>
        <h2>Rankings</h2>
        <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
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
        <div style={{ display: "flex", gap: 10, alignItems: "center", marginTop: 10 }}>
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
          <label style={{ display: "flex", alignItems: "center", gap: 6 }}>
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
      <RankingsTable rankingsData={rankingsData} />
      <MissingDobFixer athletes={missingDobAthletes} onSaveDob={fixAthleteDob} />
    </div>
  );
}

export default App;