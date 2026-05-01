import { useState } from "react";

function MissingDobFixer({ athletes, onSaveDob }) {
  const [dobByAthleteId, setDobByAthleteId] = useState({});
  const [savingId, setSavingId] = useState(null);

  if (!athletes || athletes.length === 0) {
    return null;
  }

  async function save(athleteId) {
    const dob = dobByAthleteId[athleteId];
    if (!dob) return;

    setSavingId(athleteId);
    try {
      await onSaveDob(athleteId, dob);
      setDobByAthleteId((prev) => ({ ...prev, [athleteId]: "" }));
    } finally {
      setSavingId(null);
    }
  }

  return (
    <div style={{ marginTop: 12, border: "1px solid #d1d5db", padding: 12, borderRadius: 6 }}>
      <h3 style={{ marginTop: 0 }}>Fix Missing Birthdates</h3>
      {athletes.map((a) => (
        <div key={a.id} style={{ display: "flex", gap: 8, alignItems: "center", marginBottom: 8 }}>
          <span style={{ minWidth: 220 }}>
            {a.firstName} {a.lastName} ({a.gender})
          </span>
          <input
            type="date"
            value={dobByAthleteId[a.id] ?? ""}
            onChange={(e) =>
              setDobByAthleteId((prev) => ({ ...prev, [a.id]: e.target.value }))
            }
          />
          <button onClick={() => save(a.id)} disabled={!dobByAthleteId[a.id] || savingId === a.id}>
            {savingId === a.id ? "Saving..." : "Save DOB"}
          </button>
        </div>
      ))}
    </div>
  );
}

export default MissingDobFixer;
