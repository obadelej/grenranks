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
    <div className="card section">
      <h3 className="card-title">Fix Missing Birthdates</h3>
      {athletes.map((a) => (
        <div key={a.id} className="row-wrap">
          <span className="athlete-label">
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
