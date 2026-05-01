import { useCallback, useEffect, useRef, useState } from "react";
import { fetchImportHistory, importHytekCsv } from "../api/resultsapi";
import {
  readImportHistoryPageFromUrl,
  writeImportHistoryPageToUrl,
} from "../utils/urlSync";

function formatUtc(iso) {
  if (!iso) return "";
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return String(iso);
  }
}

export default function HytekImportSection({ onImportSuccess }) {
  const pageSize = 10;
  const [file, setFile] = useState(null);
  const [loading, setLoading] = useState(false);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [message, setMessage] = useState("");
  const [history, setHistory] = useState([]);
  const [historyPage, setHistoryPage] = useState(1);
  const [historyTotalCount, setHistoryTotalCount] = useState(0);
  const formRef = useRef(null);

  const loadHistory = useCallback(async (page = 1) => {
    setHistoryLoading(true);
    setMessage("");
    try {
      const data = await fetchImportHistory({ page, pageSize });
      setHistory(Array.isArray(data.items) ? data.items : []);
      setHistoryTotalCount(data.totalCount ?? 0);
      const nextPage = data.page ?? page;
      setHistoryPage(nextPage);
      writeImportHistoryPageToUrl(nextPage);
    } catch (err) {
      setMessage(err.message);
    } finally {
      setHistoryLoading(false);
    }
  }, []);

  useEffect(() => {
    const initialPage = readImportHistoryPageFromUrl();
    void loadHistory(initialPage);

    function onPopState() {
      const page = readImportHistoryPageFromUrl();
      void loadHistory(page);
    }
    window.addEventListener("popstate", onPopState);
    return () => window.removeEventListener("popstate", onPopState);
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
      await loadHistory(1);
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
    <section className="card section">
      <h2>Hy-Tek import</h2>
      <p className="muted-text">
        Upload a Meet Manager export (.csv). Recent runs are listed below; use Refresh to reload after importing from Swagger or here.
      </p>

      <form ref={formRef} onSubmit={onSubmit} className="row-wrap section">
        <input
          type="file"
          accept=".csv"
          onChange={(ev) => setFile(ev.target.files?.[0] ?? null)}
          disabled={loading}
        />
        <button type="submit" disabled={loading || !file}>
          {loading ? "Uploading…" : "Upload CSV"}
        </button>
        <button
          type="button"
          onClick={() => loadHistory(historyPage)}
          disabled={historyLoading}
        >
          {historyLoading ? "Loading…" : "Refresh history"}
        </button>
      </form>

      {message && (
        <p className="status-message">
          <b>Import / history:</b> {message}
        </p>
      )}

      <h3 className="card-title">Recent import runs</h3>
      {history.length === 0 && !historyLoading ? (
        <p className="muted-text">No imports recorded yet.</p>
      ) : (
        <div className="table-wrap">
          <table className="data-table">
            <thead>
              <tr>
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
                <tr key={row.id}>
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
      <div className="pagination-row">
        <button
          type="button"
          onClick={() => loadHistory(historyPage - 1)}
          disabled={historyPage <= 1}
        >
          Previous
        </button>
        <span>
          Page {historyPage} of {Math.max(1, Math.ceil(historyTotalCount / pageSize))}
        </span>
        <button
          type="button"
          onClick={() => loadHistory(historyPage + 1)}
          disabled={historyPage >= Math.ceil(historyTotalCount / pageSize)}
        >
          Next
        </button>
        <span className="muted-text">Total imports: {historyTotalCount}</span>
      </div>
    </section>
  );
}
