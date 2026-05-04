import { toEventDisplayName } from "../utils/eventNames";
import { jsPDF } from "jspdf";
import autoTable from "jspdf-autotable";

function pick(obj, ...keys) {
  if (!obj) return undefined;
  for (const k of keys) {
    if (Object.prototype.hasOwnProperty.call(obj, k) && obj[k] !== undefined) {
      return obj[k];
    }
  }
  return undefined;
}

function normalizeRankingRows(rows) {
  if (!Array.isArray(rows)) return [];
  return rows.map((r) => ({
    id: pick(r, "id", "Id"),
    rank: pick(r, "rank", "Rank"),
    athleteName: pick(r, "athleteName", "AthleteName"),
    performance: pick(r, "performance", "Performance"),
    wind: pick(r, "wind", "Wind"),
    resultDate: pick(r, "resultDate", "ResultDate"),
    meetName: pick(r, "meetName", "MeetName"),
  }));
}

function formatWind(wind) {
  if (wind === null || wind === undefined) return "-";
  const numeric = Number(wind);
  if (!Number.isFinite(numeric)) return String(wind);
  return Number.isInteger(numeric) ? numeric.toFixed(1) : String(wind);
}

function RankingsDataTable({ rows }) {
  return (
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
          {(rows ?? []).map((r) => (
            <tr key={r.id}>
              <td>{r.rank}</td>
              <td>{r.athleteName}</td>
              <td>{r.performance}</td>
              <td>{formatWind(r.wind)}</td>
              <td>{new Date(r.resultDate).toLocaleDateString()}</td>
              <td>{r.meetName}</td>
            </tr>
          ))}
          {(!rows || rows.length === 0) && (
            <tr>
              <td colSpan="6" className="table-empty">
                No results in this list.
              </td>
            </tr>
          )}
        </tbody>
      </table>
    </div>
  );
}

function RankingsTable({ rankingsData }) {
  if (!rankingsData) {
    return null;
  }

  const eventDisplayName =
    pick(rankingsData, "eventDisplayName", "EventDisplayName") ||
    toEventDisplayName(pick(rankingsData, "eventName", "EventName"));
  const windSplit = Boolean(
    pick(rankingsData, "windSplitRankings", "WindSplitRankings"),
  );
  const rowsLegal = normalizeRankingRows(
    pick(rankingsData, "rankingsLegalWind", "RankingsLegalWind") ?? [],
  );
  const rowsOther = normalizeRankingRows(
    pick(rankingsData, "rankingsNoWindOrIllegalWind", "RankingsNoWindOrIllegalWind") ?? [],
  );
  const rowsSingle = normalizeRankingRows(
    pick(rankingsData, "rankings", "Rankings") ?? [],
  );
  const windSplitNote =
    pick(rankingsData, "windSplitNote", "WindSplitNote") ?? null;
  const warning = pick(rankingsData, "warning", "Warning");
  const gender = pick(rankingsData, "gender", "Gender");
  const category = pick(rankingsData, "category", "Category");
  const year = pick(rankingsData, "year", "Year");
  const bestPerAthleteOnly = pick(
    rankingsData,
    "bestPerAthleteOnly",
    "BestPerAthleteOnly",
  );
  const eventType = pick(rankingsData, "eventType", "EventType");

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
    doc.text(`Filter: ${gender} / ${category}`, 40, 58);
    doc.text(
      `Season: ${year ?? "All"} / Mode: ${bestPerAthleteOnly ? "Best per athlete" : "All results"}`,
      40,
      74,
    );
    doc.text(`Generated: ${new Date().toLocaleString()}`, 40, 90);
    if (windSplit && windSplitNote) {
      doc.text(String(windSplitNote), 40, 106, { maxWidth: 720 });
    }

    let y = windSplit && windSplitNote ? 128 : 105;
    const tableBody = (rows) =>
      (rows ?? []).map((r) => [
        r.rank ?? "",
        r.athleteName ?? "",
        r.performance ?? "",
        formatWind(r.wind),
        r.resultDate ? new Date(r.resultDate).toLocaleDateString() : "",
        r.meetName ?? "",
      ]);

    if (windSplit) {
      doc.setFontSize(11);
      doc.text("Legal wind (reading present, ≤ +2.0 m/s)", 40, y);
      y += 18;
      autoTable(doc, {
        startY: y,
        head: [["Rank", "Athlete", "Performance", "Wind", "Date", "Meet"]],
        body: tableBody(rowsLegal),
        styles: { fontSize: 9, cellPadding: 4 },
        headStyles: { fillColor: [37, 99, 235] },
        alternateRowStyles: { fillColor: [248, 250, 252] },
      });
      y = doc.lastAutoTable.finalY + 24;
      doc.setFontSize(11);
      doc.text("No wind reading or illegal wind (> +2.0 m/s)", 40, y);
      y += 18;
      autoTable(doc, {
        startY: y,
        head: [["Rank", "Athlete", "Performance", "Wind", "Date", "Meet"]],
        body: tableBody(rowsOther),
        styles: { fontSize: 9, cellPadding: 4 },
        headStyles: { fillColor: [180, 83, 9] },
        alternateRowStyles: { fillColor: [254, 243, 199] },
      });
    } else {
      autoTable(doc, {
        startY: y,
        head: [["Rank", "Athlete", "Performance", "Wind", "Date", "Meet"]],
        body: tableBody(rowsSingle),
        styles: { fontSize: 9, cellPadding: 4 },
        headStyles: { fillColor: [37, 99, 235] },
        alternateRowStyles: { fillColor: [248, 250, 252] },
      });
    }

    const safeEventName = String(eventDisplayName).replace(/[^a-z0-9_-]+/gi, "-");
    const safeCategory = String(category).replace(/[^a-z0-9_-]+/gi, "-");
    const safeGender = String(gender).replace(/[^a-z0-9_-]+/gi, "-");
    const safeYear = String(year ?? "all");
    const suffix = windSplit ? "-wind-split" : "";
    doc.save(`rankings-${safeEventName}-${safeGender}-${safeCategory}-${safeYear}${suffix}.pdf`);
  }

  return (
    <section className="card section">
      <h2>
        Rankings - {eventDisplayName} ({eventType})
      </h2>
      <p>
        Filter: {gender} / {category}
      </p>
      <p>
        Season: {year ?? "All"} / Mode:{" "}
        {bestPerAthleteOnly ? "Best per athlete" : "All results"}
      </p>
      {windSplit && windSplitNote && (
        <p className="muted-text" style={{ marginTop: "0.35rem" }}>
          {windSplitNote}
        </p>
      )}
      <div className="row-wrap">
        <button type="button" onClick={downloadPdf}>
          Save Rankings as PDF
        </button>
      </div>
      {warning && (
        <p className="warning-text">
          {warning}
        </p>
      )}

      {windSplit ? (
        <>
          <h3 className="card-title" style={{ marginTop: "0.75rem" }}>
            Legal wind (reading present, ≤ +2.0 m/s)
          </h3>
          <RankingsDataTable rows={rowsLegal} />
          <h3 className="card-title" style={{ marginTop: "0.75rem" }}>
            No wind reading or illegal wind (over +2.0 m/s)
          </h3>
          <RankingsDataTable rows={rowsOther} />
        </>
      ) : (
        <RankingsDataTable rows={rowsSingle} />
      )}
    </section>
  );
}

export default RankingsTable;
