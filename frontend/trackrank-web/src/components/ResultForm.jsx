function ResultForm({
  form,
  onChange,
  onSubmit,
  loading,
  editingId,
  onCancelEdit,
}) {
  return (
    <form onSubmit={onSubmit} style={{ display: "grid", gap: 10, marginBottom: 20 }}>
      <input
        name="performance"
        placeholder="Performance (e.g. 10.84)"
        value={form.performance}
        onChange={onChange}
        required
      />
      <input
        name="wind"
        placeholder="Wind (optional, e.g. 1.2)"
        value={form.wind}
        onChange={onChange}
      />
      <input type="date" name="resultDate" value={form.resultDate} onChange={onChange} required />

      <div style={{ display: "flex", gap: 10 }}>
        <button type="submit" disabled={loading}>
          {loading ? "Saving..." : editingId === null ? "Save Result" : "Update Result"}
        </button>

        {editingId !== null && (
          <button type="button" onClick={onCancelEdit}>
            Cancel Edit
          </button>
        )}
      </div>
    </form>
  );
}

export default ResultForm;
