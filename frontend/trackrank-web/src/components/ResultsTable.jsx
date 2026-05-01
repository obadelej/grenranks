function ResultsTable({ results, onEdit, onDelete }) {
  return (
    <>
      <h2>Results</h2>
      <table border="1" cellPadding="8" style={{ borderCollapse: "collapse", width: "100%" }}>
        <thead>
          <tr>
            <th>ID</th>
            <th>Athlete</th>
            <th>Meet</th>
            <th>Event</th>
            <th>Performance</th>
            <th>Wind</th>
            <th>Date</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {results.map((r) => (
            <tr key={r.id}>
              <td>{r.id}</td>
              <td>{r.athleteName}</td>
              <td>{r.meetName}</td>
              <td>{r.eventName}</td>
              <td>{r.performance}</td>
              <td>{r.wind ?? "-"}</td>
              <td>{new Date(r.resultDate).toLocaleDateString()}</td>
              <td>
                <button onClick={() => onEdit(r)} style={{ marginRight: 8 }}>
                  Edit
                </button>
                <button onClick={() => onDelete(r.id)}>Delete</button>
              </td>
            </tr>
          ))}
          {results.length === 0 && (
            <tr>
              <td colSpan="8" style={{ textAlign: "center" }}>
                No results yet.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </>
  );
}

export default ResultsTable;
