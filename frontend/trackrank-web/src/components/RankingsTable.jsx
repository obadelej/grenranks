function RankingsTable({ rankingsData }) {
  if (!rankingsData) {
    return null;
  }

  const rows = rankingsData.rankings ?? [];

  return (
    <div style={{ marginTop: 24 }}>
      <h2>
        Rankings - {rankingsData.eventName} ({rankingsData.eventType})
      </h2>
      <p>
        Filter: {rankingsData.gender} / {rankingsData.category}
      </p>
      <p>
        Season: {rankingsData.year ?? "All"} / Mode:{" "}
        {rankingsData.bestPerAthleteOnly ? "Best per athlete" : "All results"}
      </p>
      {rankingsData.warning && (
        <p style={{ color: "#b45309" }}>
          {rankingsData.warning}
        </p>
      )}

      <table border="1" cellPadding="8" style={{ borderCollapse: "collapse", width: "100%" }}>
        <thead>
          <tr>
            <th>Rank</th>
            <th>Athlete</th>
            <th>Performance</th>
            <th>Wind</th>
            <th>Date</th>
            <th>Meet</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.id}>
              <td>{r.rank}</td>
              <td>{r.athleteName}</td>
              <td>{r.performance}</td>
              <td>{r.wind ?? "-"}</td>
              <td>{new Date(r.resultDate).toLocaleDateString()}</td>
              <td>{r.meetName}</td>
            </tr>
          ))}
          {rows.length === 0 && (
            <tr>
              <td colSpan="6" style={{ textAlign: "center" }}>
                No rankings yet for this event.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

export default RankingsTable;
