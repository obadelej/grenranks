import { toEventDisplayName } from "../utils/eventNames";

function ResultsTable({ results, onEdit, onDelete }) {
  return (
    <section className="card">
      <h2>Results</h2>
      <div className="table-wrap">
        <table className="data-table">
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
                <td>{r.eventDisplayName || toEventDisplayName(r.eventName)}</td>
                <td>{r.performance}</td>
                <td>{r.wind ?? "-"}</td>
                <td>{new Date(r.resultDate).toLocaleDateString()}</td>
                <td>
                  <div className="row-wrap tight">
                    <button onClick={() => onEdit(r)}>Edit</button>
                    <button onClick={() => onDelete(r.id)}>Delete</button>
                  </div>
                </td>
              </tr>
            ))}
            {results.length === 0 && (
              <tr>
                <td colSpan="8" className="table-empty">
                  No results yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default ResultsTable;
