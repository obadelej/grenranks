import { useEffect, useState } from "react";
import LookupForms from "./components/LookupForms";
import ResultForm from "./components/ResultForm";
import ResultsTable from "./components/ResultsTable";
import RankingsTable from "./components/RankingsTable";
import MissingDobFixer from "./components/MissingDobFixer";
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

function App() {
  const [form, setForm] = useState(initialForm);
  const [results, setResults] = useState([]);
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

  async function loadResults() {
    try {
      const data = await fetchResults();
      setResults(data);
    } catch (err) {
      setMessage(err.message);
    }
  }

  useEffect(() => {
    loadLookups();
    loadResults();
  }, []);

  function onChange(e) {
    const { name, value } = e.target;
    setForm((prev) => ({ ...prev, [name]: value }));
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
      await loadResults();
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
      await loadResults();
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
      await loadResults();
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