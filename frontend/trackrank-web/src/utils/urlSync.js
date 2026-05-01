const RESULTS = {
  page: "resultsPage",
  athleteId: "resultsAthleteId",
  eventId: "resultsEventId",
  year: "resultsYear",
  sourceType: "resultsSourceType",
  sortBy: "resultsSortBy",
  sortDir: "resultsSortDir",
};

const IMPORT_HISTORY_PAGE = "importHistoryPage";

function replaceUrlSearch(nextParams) {
  const qs = nextParams.toString();
  const { pathname, hash } = window.location;
  const url = qs ? `${pathname}?${qs}${hash}` : `${pathname}${hash}`;
  window.history.replaceState(null, "", url);
}

export function readResultsUrlState() {
  const p = new URLSearchParams(window.location.search);
  const rawPage = parseInt(p.get(RESULTS.page) || "1", 10);
  const page = Number.isFinite(rawPage) && rawPage > 0 ? rawPage : 1;
  return {
    page,
    filters: {
      athleteId: p.get(RESULTS.athleteId) || "",
      eventId: p.get(RESULTS.eventId) || "",
      year: p.get(RESULTS.year) || "",
      sourceType: p.get(RESULTS.sourceType) || "",
      sortBy: p.get(RESULTS.sortBy) || "resultDate",
      sortDir: p.get(RESULTS.sortDir) || "desc",
    },
  };
}

export function writeResultsUrlState({ page, filters }) {
  const params = new URLSearchParams(window.location.search);

  const setOrDelete = (key, value, omitWhen) => {
    if (value === omitWhen || value === "" || value === null || value === undefined) {
      params.delete(key);
    } else {
      params.set(key, String(value));
    }
  };

  setOrDelete(RESULTS.page, page, 1);
  setOrDelete(RESULTS.athleteId, filters.athleteId, "");
  setOrDelete(RESULTS.eventId, filters.eventId, "");
  setOrDelete(RESULTS.year, filters.year, "");
  setOrDelete(RESULTS.sourceType, filters.sourceType, "");
  setOrDelete(RESULTS.sortBy, filters.sortBy, "resultDate");
  setOrDelete(RESULTS.sortDir, filters.sortDir, "desc");

  replaceUrlSearch(params);
}

export function readImportHistoryPageFromUrl() {
  const p = new URLSearchParams(window.location.search);
  const raw = parseInt(p.get(IMPORT_HISTORY_PAGE) || "1", 10);
  return Number.isFinite(raw) && raw > 0 ? raw : 1;
}

export function writeImportHistoryPageToUrl(page) {
  const params = new URLSearchParams(window.location.search);
  if (page <= 1) {
    params.delete(IMPORT_HISTORY_PAGE);
  } else {
    params.set(IMPORT_HISTORY_PAGE, String(page));
  }
  replaceUrlSearch(params);
}
