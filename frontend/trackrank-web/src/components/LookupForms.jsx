function LookupForms({ seedData, athletes, meets, events, form, onChange }) {
  return (
    <>
      <button onClick={seedData} style={{ marginBottom: 16 }}>
        Seed Test Data
      </button>

      <div style={{ marginBottom: 20 }}>
        <label>
          Athlete
          <select name="athleteId" value={form.athleteId} onChange={onChange} required>
            <option value="" disabled>
              Select athlete
            </option>
            {athletes.map((a) => (
              <option key={a.id} value={a.id}>
                {a.name}
              </option>
            ))}
          </select>
        </label>

        <label style={{ marginLeft: 12 }}>
          Meet
          <select name="meetId" value={form.meetId} onChange={onChange} required>
            <option value="" disabled>
              Select meet
            </option>
            {meets.map((m) => (
              <option key={m.id} value={m.id}>
                {m.name} ({new Date(m.meetDate).toLocaleDateString()})
              </option>
            ))}
          </select>
        </label>

        <label style={{ marginLeft: 12 }}>
          Event
          <select name="eventId" value={form.eventId} onChange={onChange} required>
            <option value="" disabled>
              Select event
            </option>
            {events.map((ev) => (
              <option key={ev.id} value={ev.id}>
                {ev.name}
              </option>
            ))}
          </select>
        </label>
      </div>
    </>
  );
}

export default LookupForms;
