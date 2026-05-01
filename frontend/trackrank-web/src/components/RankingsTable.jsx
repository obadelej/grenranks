import { toEventDisplayName } from "../utils/eventNames";
import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";

function RankingsTable({ rankingsData }) {
  if (!rankingsData) {
    return null;
  }

  const rows = rankingsData.rankings ?? [];
  const eventDisplayName = rankingsData.eventDisplayName || toEventDisplayName(rankingsData.eventName);

  function formatWind(wind) {
    if (wind === null || wind === undefined) return "-";
    const numeric = Number(wind);
    if (!Number.isFinite(numeric)) return String(wind);
    return Number.isInteger(numeric) ? numeric.toFixed(1) : String(wind);
  }

  function downloadPdf() {
    const doc = new jsPDF({
      orientation: "landscape",
      unit: "pt",
      format: "a4",
    });

    const title = `Grenada Track & Field Rankings - ${eventDisplayName}`;
    doc.setFontSize(14);
    doc.text(title, 40, 40);
    doc.setFontSize(10);
    doc.text(`Filter: ${rankingsData.gender} / ${rankingsData.category}`, 40, 58);
    doc.text(
      `Season: ${rankingsData.year ?? "All"} / Mode: ${rankingsData.bestPerAthleteOnly ? "Best per athlete" : "All results"}`,
      40,
      74,
    );
    doc.text(`Generated: ${new Date().toLocaleString()}`, 40, 90);

    autoTable(doc, {
      startY: 105,
      head: [["Rank", "Athlete", "Performance", "Wind", "Date", "Meet"]],
      body: rows.map((r) => [
        r.rank ?? "",
        r.athleteName ?? "",
        r.performance ?? "",
        formatWind(r.wind),
        r.resultDate ? new Date(r.resultDate).toLocaleDateString() : "",
        r.meetName ?? "",
      ]),
      styles: { fontSize: 9, cellPadding: 4 },
      headStyles: { fillColor: [37, 99, 235] },
      alternateRowStyles: { fillColor: [248, 250, 252] },
    });

    const safeEventName = String(eventDisplayName).replace(/[^a-z0-9_-]+/gi, "-");
    const safeCategory = String(rankingsData.category).replace(/[^a-z0-9_-]+/gi, "-");
    const safeGender = String(rankingsData.gender).replace(/[^a-z0-9_-]+/gi, "-");
    const safeYear = String(rankingsData.year ?? "all");
    doc.save(`rankings-${safeEventName}-${safeGender}-${safeCategory}-${safeYear}.pdf`);
  }

  return (
    <section className="card section">
      <h2>
        Rankings - {eventDisplayName} ({rankingsData.eventType})
      </h2>
      <p>
        Filter: {rankingsData.gender} / {rankingsData.category}
      </p>
      <p>
        Season: {rankingsData.year ?? "All"} / Mode:{" "}
        {rankingsData.bestPerAthleteOnly ? "Best per athlete" : "All results"}
      </p>
      <div className="row-wrap">
        <button type="button" onClick={downloadPdf}>
          Save Rankings as PDF
        </button>
      </div>
      {rankingsData.warning && (
        <p className="warning-text">
          {rankingsData.warning}
        </p>
      )}

      <div className="table-wrap">
        <table className="data-table">
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
                <td>{formatWind(r.wind)}</td>
                <td>{new Date(r.resultDate).toLocaleDateString()}</td>
                <td>{r.meetName}</td>
              </tr>
            ))}
            {rows.length === 0 && (
              <tr>
                <td colSpan="6" className="table-empty">
                  No rankings yet for this event.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default RankingsTable;
