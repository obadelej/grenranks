import { useCallback, useEffect, useRef, useState } from "react";
import { fetchImportHistory, importHytekCsv } from "../api/resultsapi";

function formatUtc(iso) {
  if (!iso) return "";
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return String(iso);
  }
}

export default function HytekImportSection({ onImportSuccess }) {
  const [file, setFile] = useState(null);
  const [loading, setLoading] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [history, setHistory] = useState([]);
  const formRef = useRef(null);

  const loadHistory = useCallback(async () => {
    setHistoryLoading(true);
    setMessage("");
    try {
      const rows = await fetchImportHistory(15);
      setHistory(Array.isArray(rows) ? rows : []);
    } catch (err) {
      setMessage(err.message);
    } finally {
      setHistoryLoading(false);
    }
  }, []);

  useEffect(() => {
    loadHistory();
  }, [loadHistory]);

  async function onSubmit(e) {
    e.preventDefault();
    if (!file) {
      setMessage("Choose a .csv file first.");
      return;
    }
    if (!file.name.toLowerCase().endsWith(".csv")) {
      setMessage("Only .csv files are supported.");
      return;
    }

    setLoading(true);
    setMessage("");
    try {
      const summary = await importHytekCsv(file);
      setFile(null);
      formRef.current?.reset();
      setMessage(
        `Import finished: ${summary.importedCount ?? 0} new, ${summary.skippedCount ?? 0} skipped, ${summary.errorCount ?? 0} errors (parsed ${summary.parsedRows ?? 0} rows).`,
      );
      await loadHistory();
      if (typeof onImportSuccess === "function") {
        onImportSuccess(summary);
      }
    } catch (err) {
      setMessage(err.message);
    } finally {
      setLoading(false);
    }
  }

  return (
    <section style={{ marginTop: 28, paddingTop: 16, borderTop: "1px solid #ccc" }}>
      <h2>Hy-Tek import</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Upload a Meet Manager export (.csv). Recent runs are listed below; use Refresh to reload after importing from Swagger or here.
      </p>

      <form ref={formRef} onSubmit={onSubmit} style={{ marginBottom: 16 }}>
        <input
          type="file"
          accept=".csv"
          onChange={(ev) => setFile(ev.target.files?.[0] ?? null)}
          disabled={loading}
        />
        <button type="submit" disabled={loading || !file} style={{ marginLeft: 8 }}>
          {loading ? "Uploading…" : "Upload CSV"}
        </button>
        <button
          type="button"
          onClick={loadHistory}
          disabled={historyLoading}
          style={{ marginLeft: 8 }}
        >
          {historyLoading ? "Loading…" : "Refresh history"}
        </button>
      </form>

      {message && (
        <p style={{ marginBottom: 12 }}>
          <b>Import / history:</b> {message}
        </p>
      )}

      <h3 style={{ fontSize: 16 }}>Recent import runs</h3>
      {history.length === 0 && !historyLoading ? (
        <p style={{ color: "#666" }}>No imports recorded yet.</p>
      ) : (
        <div style={{ overflowX: "auto" }}>
          <table
            cellPadding={6}
            style={{ borderCollapse: "collapse", width: "100%", fontSize: 13 }}
          >
            <thead>
              <tr style={{ background: "#f0f0f0" }}>
                <th align="left">When (UTC)</th>
                <th align="left">File</th>
                <th align="right">Parsed</th>
                <th align="right">Imported</th>
                <th align="right">Skipped</th>
                <th align="right">Errors</th>
                <th align="right">Track</th>
                <th align="right">Field</th>
              </tr>
            </thead>
            <tbody>
              {history.map((row) => (
                <tr key={row.id} style={{ borderBottom: "1px solid #eee" }}>
                  <td>{formatUtc(row.importedAtUtc)}</td>
                  <td>{row.fileName}</td>
                  <td align="right">{row.parsedRows}</td>
                  <td align="right">{row.importedCount}</td>
                  <td align="right">{row.skippedCount}</td>
                  <td align="right">{row.errorCount}</td>
                  <td align="right">{row.trackParsedCount}</td>
                  <td align="right">{row.fieldParseCount}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
